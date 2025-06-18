using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using Microsoft.IdentityModel.Tokens;
using ClosedXML.Excel;

namespace Inicio_Sesion_API.Controllers;

[ApiController]
[Route("logs")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public LogsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
[HttpGet("log_api")]
public async Task<IActionResult> GetLogApi(
    [FromHeader(Name = "Authorization")] string token,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var stopwatch = Stopwatch.StartNew();
    var tiempoInicio = DateTime.UtcNow;
    string query = "SELECT * FROM log_api";  // Puedes ajustar este query según tus necesidades

    // Validar el token del administrador
    if (!await ValidateAdminTokenAsync(token))
    {
        var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        var executionTime = DateTime.UtcNow - tiempoInicio;
        
        // Llamada a CrearLogApi cuando el token es inválido
        await CrearLogApi("GET", "/logs/log_api", "401", token, parametrosSalida, executionTime.TotalMilliseconds.ToString(), query);
        return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
    }

    try
    {
        var totalItems = await _context.LogApi.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var logs = await _context.LogApi
            .OrderByDescending(l => l.created_at)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync() ?? new List<LogApi>();

        var response = new
        {
            totalItems,
            pageSize,
            currentPage = page,
            totalPages,
            data = logs.Select(l => new
            {
                l.log_api_id,
                l.log_api_metodo,
                l.log_api_url,
                l.log_api_tipo_respuesta,
                l.log_api_fecha,
                // Limitar la longitud de los parámetros
                log_api_parametros_entrada = TruncateString(CleanJsonString(l.log_api_parametros_entrada), 2000),
                log_api_parametros_salida = TruncateString(CleanJsonString(l.log_api_parametros_salida), 2000),
                l.log_api_tiempo_ejecucion,
                log_api_query = CleanJsonString(l.log_api_query),
                created_at = l.created_at.ToString("yyyy-MM-dd HH:mm:ss") // Formato de fecha
            })
        };

        var executionTime = DateTime.UtcNow - tiempoInicio;
        // Llamada a CrearLogApi después de obtener los resultados
        await CrearLogApi("GET", "/logs/log_api", "200", token, JsonConvert.SerializeObject(response), executionTime.TotalMilliseconds.ToString(), query);
        
        var administradorId = await ObtenerAdministradorIdDesdeToken(token);
        // Llamada a CrearLogSistema para registrar la acción
        await CrearLogSistema("Consulta logs", administradorId, JsonConvert.SerializeObject(response));

        return Ok(response);
    }
    catch (Exception ex)
    {
        var errorExecutionTime = DateTime.UtcNow - tiempoInicio;
        await CrearLogApi("GET", "/logs/log_api", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
        return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
    }
}


    [HttpGet("log_api_filtro")]
    public async Task<IActionResult> FiltrarLogApi(
        [FromHeader(Name = "Authorization")] string token,
        [FromQuery] string? metodo,
        [FromQuery] string? url,
        [FromQuery] string? tipoRespuesta,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? parametrosEntrada,
        [FromQuery] string? parametrosSalida,
        [FromQuery] string? queryApi,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var startTime = DateTime.UtcNow;
        string query = "SELECT * FROM log_api WHERE 1=1";


        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/log_api/filtrar", "401", token, "Token no autorizado", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado" });
        }

        try
        {
            var queryBase = _context.LogApi.AsQueryable();

            if (!string.IsNullOrEmpty(metodo))
            {
                queryBase = queryBase.Where(l => l.log_api_metodo.Contains(metodo));
                query += $" AND log_api_metodo LIKE '%{metodo}%'";
            }

            if (!string.IsNullOrEmpty(url))
            {
                queryBase = queryBase.Where(l => l.log_api_url.Contains(url));
                query += $" AND log_api_url LIKE '%{url}%'";
            }

            if (!string.IsNullOrEmpty(tipoRespuesta))
            {
                queryBase = queryBase.Where(l => l.log_api_tipo_respuesta.Contains(tipoRespuesta));
                query += $" AND log_api_tipo_respuesta LIKE '%{tipoRespuesta}%'";
            }

            if (fechaInicio.HasValue)
            {
                queryBase = queryBase.Where(l => l.log_api_fecha >= fechaInicio.Value);
                query += $" AND log_api_fecha >= '{fechaInicio.Value:yyyy-MM-dd HH:mm:ss}'";
            }

            if (fechaFin.HasValue)
            {
                queryBase = queryBase.Where(l => l.log_api_fecha <= fechaFin.Value);
                query += $" AND log_api_fecha <= '{fechaFin.Value:yyyy-MM-dd HH:mm:ss}'";
            }

            if (!string.IsNullOrEmpty(parametrosEntrada))
            {
                queryBase = queryBase.Where(l => l.log_api_parametros_entrada.Contains(parametrosEntrada));
                query += $" AND log_api_parametros_entrada LIKE '%{parametrosEntrada}%'";
            }

            if (!string.IsNullOrEmpty(parametrosSalida))
            {
                queryBase = queryBase.Where(l => l.log_api_parametros_salida.Contains(parametrosSalida));
                query += $" AND log_api_parametros_salida LIKE '%{parametrosSalida}%'";
            }

            if (!string.IsNullOrEmpty(queryApi))
            {
                queryBase = queryBase.Where(l => l.log_api_query.Contains(queryApi));
                query += $" AND log_api_query LIKE '%{queryApi}%'";
            }

            var totalItems = await queryBase.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var logs = await queryBase
                .OrderBy(l => l.log_api_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            query += $" ORDER BY log_api_id ASC LIMIT {(page - 1) * pageSize}, {pageSize};";

            var response = new
            {
                totalItems,
                pageSize,
                currentPage = page,
                totalPages,
                data = logs
            };

            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/log_api/filtrar", "200", token, JsonConvert.SerializeObject(response), executionTime.TotalMilliseconds.ToString(), query);
            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("filtrar Log Api", administradorId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/log_api/filtrar", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }

    [HttpGet("exportar/log_api")]
    public async Task<IActionResult> ExportarLogApi(
        [FromHeader(Name = "Authorization")] string token,
        [FromQuery] string? metodo,
        [FromQuery] string? url,
        [FromQuery] string? tipoRespuesta,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? parametrosEntrada,
        [FromQuery] string? parametrosSalida,
        [FromQuery] string? queryTexto)
    {
        var startTime = DateTime.UtcNow;
        string query = "SELECT * FROM log_api WHERE 1=1";

        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/exportar/log_api", "401", token, "Token no autorizado", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado" });
        }

        try
        {
            var queryBase = _context.LogApi.AsQueryable();

            if (!string.IsNullOrEmpty(metodo))
                queryBase = queryBase.Where(l => l.log_api_metodo.Contains(metodo));
            if (!string.IsNullOrEmpty(url))
                queryBase = queryBase.Where(l => l.log_api_url.Contains(url));
            if (!string.IsNullOrEmpty(tipoRespuesta))
                queryBase = queryBase.Where(l => l.log_api_tipo_respuesta.Contains(tipoRespuesta));
            if (fechaInicio.HasValue)
                queryBase = queryBase.Where(l => l.log_api_fecha >= fechaInicio.Value);
            if (fechaFin.HasValue)
                queryBase = queryBase.Where(l => l.log_api_fecha <= fechaFin.Value);
            if (!string.IsNullOrEmpty(parametrosEntrada))
                queryBase = queryBase.Where(l => l.log_api_parametros_entrada.Contains(parametrosEntrada));
            if (!string.IsNullOrEmpty(parametrosSalida))
                queryBase = queryBase.Where(l => l.log_api_parametros_salida.Contains(parametrosSalida));
            if (!string.IsNullOrEmpty(queryTexto))
                queryBase = queryBase.Where(l => l.log_api_query.Contains(queryTexto));

            var logs = await queryBase.ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Método,URL,Tipo Respuesta,Fecha,Parámetros Entrada,Parámetros Salida,Tiempo Ejecución,Query,Fecha Creación");

            foreach (var log in logs)
            {
                sb.AppendLine($"{log.log_api_id}," +
                              $"\"{log.log_api_metodo}\"," +
                              $"\"{log.log_api_url}\"," +
                              $"\"{log.log_api_tipo_respuesta}\"," +
                              $"{log.log_api_fecha?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                              $"\"{log.log_api_parametros_entrada}\"," +
                              $"\"{log.log_api_parametros_salida}\"," +
                              $"{log.log_api_tiempo_ejecucion}," +
                              $"\"{log.log_api_query}\"," +
                              $"{log.created_at}");
            }

            var content = Encoding.UTF8.GetBytes(sb.ToString());
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/exportar/log_api", "200", token, "Archivo generado", executionTime.TotalMilliseconds.ToString(), query);
            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Exportar Log Api", administradorId);
            return File(content, "text/csv", "log_api.csv");
        }
        catch (Exception ex)
        {
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/exportar/log_api", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }


    [HttpDelete("log_api/borrar")]
    public async Task<IActionResult> DeleteLogApi([FromHeader(Name = "Authorization")] string token)
    {
        var stopwatch = Stopwatch.StartNew();
        var tiempoInicio = DateTime.UtcNow;
        string query = "";


        if (!await ValidateAdminTokenAsync(token))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            await CrearLogApi("DELETE", "/logs/log_api", "401", token, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        try
        {
            // Eliminar todos los registros de log_api
            var logsEliminados = await _context.LogApi.CountAsync();
            _context.LogApi.RemoveRange(_context.LogApi);
            await _context.SaveChangesAsync();

            // logs
            query = " DELETE from log_api";
            var mensajeSalida = $"Se han eliminado {logsEliminados} registros de log_api.";
            await CrearLogApi("DELETE", "/logs/log_api", "200", token, mensajeSalida, stopwatch.Elapsed.TotalMilliseconds.ToString(), query);
            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Eliminar registros de Log Api", administradorId);
            return Ok(new { mensaje = mensajeSalida });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await CrearLogApi("DELETE", "/logs/log_api", "500", token, ex.ToString(), stopwatch.Elapsed.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
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
    // Función para limpiar las barras invertidas
    private string CleanJsonString(string input)
    {
        return input?.Replace("\\", ""); // Elimina las barras invertidas
    }
    private string TruncateString(string input, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Length > maxLength ? input.Substring(0, maxLength) : input;
    }

}
