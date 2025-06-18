using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using ClosedXML.Excel;

namespace Inicio_Sesion_API.Controllers;

[ApiController]
[Route("Configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public ConfiguracionController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private bool ValidateTokenFormat(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateAdminTokenAsync(string token)
    {
        if (!ValidateTokenFormat(token))
            return false;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring(7);

        var tokenEntry = await _context.Token.FirstOrDefaultAsync(t => t.token == token && t.administrador_id != null);
        return tokenEntry != null;
    }

   

[HttpPut("editar")]
public async Task<IActionResult> EditarConfiguracion([FromHeader(Name = "Authorization")] string token, [FromBody] Configuracion updatedConfig)
{
    var parametrosEntrada = JsonConvert.SerializeObject(new { token, updatedConfig });
    var tiempoInicio = DateTime.UtcNow;
    string query = "";

    try
    {
       
        if (!await ValidateAdminTokenAsync(token))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            await CrearLogApi("PUT", "/configuracion/editar", "401", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        
        var configuracion = await _context.Configuracion.FirstOrDefaultAsync();
        if (configuracion == null)
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Configuración no encontrada" });
            await CrearLogApi("PUT", "/configuracion/editar", "404", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return NotFound(new { mensaje = "Configuración no encontrada" });
        }

        // Registrar cambios realizados
        string cambiosRealizados = "";
        if (configuracion.configuracion_log_enable != updatedConfig.configuracion_log_enable)
            cambiosRealizados += $"Configuración 'log_enable' cambiada de {configuracion.configuracion_log_enable} a {updatedConfig.configuracion_log_enable}. ";
        if (configuracion.configuracion_nivel_log_api != updatedConfig.configuracion_nivel_log_api)
            cambiosRealizados += $"Configuración 'nivel_log_api' cambiada de {configuracion.configuracion_nivel_log_api} a {updatedConfig.configuracion_nivel_log_api}. ";
        if (configuracion.configuracion_tiempo_retencion != updatedConfig.configuracion_tiempo_retencion)
            cambiosRealizados += $"Configuración 'tiempo_retencion' cambiada de {configuracion.configuracion_tiempo_retencion} a {updatedConfig.configuracion_tiempo_retencion}. ";

        // Actualizar los campos
        configuracion.configuracion_log_enable = updatedConfig.configuracion_log_enable;
        configuracion.configuracion_nivel_log_api = updatedConfig.configuracion_nivel_log_api;
        configuracion.configuracion_tiempo_retencion = updatedConfig.configuracion_tiempo_retencion;
        configuracion.update_at = DateTime.UtcNow;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _context.Configuracion.Update(configuracion);
        await _context.SaveChangesAsync();
        stopwatch.Stop();

        query = $"UPDATE configuracion SET " +
            $"`configuracion_log_enable` = '{configuracion.configuracion_log_enable}', " +
            $"`configuracion_nivel_log_api` = '{configuracion.configuracion_nivel_log_api}', " +
            $"`configuracion_tiempo_retencion` = '{configuracion.configuracion_tiempo_retencion}', " +
            $"`update_at` = '{configuracion.update_at:yyyy-MM-dd HH:mm:ss}' " +
            $"WHERE `configuracion_id` = {configuracion.configuracion_id};";

        //logs
        
        var parametrosSalidaExito = JsonConvert.SerializeObject(new { mensaje = "Configuración actualizada exitosamente" });
        await CrearLogApi("PUT", "/configuracion/editar", "200", parametrosEntrada, parametrosSalidaExito, stopwatch.ElapsedMilliseconds.ToString(), query);

        
        var administradorId = await ObtenerAdministradorIdDesdeToken(token);
        await CrearLogSistema("Editar Configuración", administradorId, cambiosRealizados);

        return Ok(new { mensaje = "Configuración actualizada exitosamente" });
    }
    catch (Exception ex)
    {
        var parametrosSalidaError = JsonConvert.SerializeObject(new { mensaje = "Error interno del servidor.", error = ex.Message });
        await CrearLogApi("PUT", "/configuracion/editar", "500", parametrosEntrada, parametrosSalidaError, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), ex.Message);

        return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
    }
}

private async Task<int> ObtenerAdministradorIdDesdeToken(string token)
{
    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        token = token.Substring(7);

    var tokenEntry = await _context.Token
        .Where(t => t.token == token && t.administrador_id != null)
        .Select(t => t.administrador_id)
        .FirstOrDefaultAsync();

    return tokenEntry ?? 0; // Retorna el ID si existe, sino 0
}

     // Método para crear un log de la API
    private async Task CrearLogApi(string metodo, string url, string tipoRespuesta, string parametrosEntrada, string parametrosSalida, string tiempoEjecucion, string query)
    {
        var configuracion = await _context.Configuracion.FirstOrDefaultAsync();
        if (configuracion?.configuracion_log_enable == true)
        {
            if (configuracion.configuracion_log_enable && configuracion.configuracion_nivel_log_api == 0)
            {

                query = "";
            }

            var log = new LogApi
            {
                log_api_metodo = metodo,
                log_api_url = url,
                log_api_tipo_respuesta = tipoRespuesta,
                log_api_fecha = DateTime.UtcNow,
                log_api_parametros_entrada = parametrosEntrada,
                log_api_parametros_salida = parametrosSalida,
                log_api_tiempo_ejecucion = tiempoEjecucion,
                log_api_query = query,
                created_at = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds
            };

            _context.LogApi.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    
private async Task CrearLogSistema(string accion, int administradorId, string? cambiosRealizados = "")
{
    try
    {
        var log = new LogSistema
        {
            log_sistema_accion = accion,
            log_sistema_fecha = DateTime.UtcNow,
            log_sistema_cambios_realizados = cambiosRealizados,
            administrador_id = administradorId,
            created_at = DateTime.UtcNow
        };

        _context.LogSistema.Add(log);
        await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al registrar en LogSistema: {ex.Message}");
    }
}
   
}
