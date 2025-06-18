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
using System.Linq;

namespace Inicio_Sesion_API.Controllers
{
    [ApiController]
    [Route("logs")]
    public class LogSistemaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public LogSistemaController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }



[HttpGet("log_sistema")]
public async Task<IActionResult> GetLogSistema([FromHeader(Name = "Authorization")] string token, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    var stopwatch = Stopwatch.StartNew();
    var tiempoInicio = DateTime.UtcNow;
    string query = "";

   
    if (!await ValidateAdminTokenAsync(token))
    {
        stopwatch.Stop(); 
        var executionTime = stopwatch.Elapsed; 

        var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        await CrearLogApi("Obtener Log Sistema", "/logs/log_sistema", "401", token, "", executionTime.TotalMilliseconds.ToString(), "");

        return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
    }

    try
    {
        var totalItems = await _context.LogSistema.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        // Incluir la información del Administrador
        var logs = await _context.LogSistema
            .Include(l => l.Administrador)  
            .OrderByDescending(l => l.created_at)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync() ?? new List<LogSistema>();

        var response = new
        {
            totalItems,
            pageSize,
            currentPage = page,
            totalPages,
            data = logs.Select(l => new
            {
                l.log_sistema_id,
                l.log_sistema_accion,
                l.log_sistema_fecha,
                l.log_sistema_cambios_realizados,
                created_at = l.created_at.ToString("yyyy-MM-dd HH:mm:ss"), 
                administrador = new
                {
                    nombre = l.Administrador.administrador_nombres,
                    apellido = l.Administrador.administrador_apellidos
                }
            })
        };

        stopwatch.Stop(); 
        var executionTime = stopwatch.Elapsed; 
        string responseJson = JsonConvert.SerializeObject(response);

        query = "SELECT * FROM log_sistema ORDER BY created_at DESC LIMIT @pageSize OFFSET @offset;";

        var administradorId = await ObtenerAdministradorIdDesdeToken(token);
        await CrearLogSistema("Obtener Log Sistema", administradorId);

        await CrearLogApi("GET", "/logs/log_sistema_filtro", "200", token, "Archivo csv generado", executionTime.TotalMilliseconds.ToString(), query);

        return Ok(response);
    }
    catch (Exception ex)
    {
        stopwatch.Stop(); 
        var executionTime = stopwatch.Elapsed; 

        await CrearLogApi("GET", "/logs/log_sistema", "500", token, ex.ToString(), executionTime.TotalMilliseconds.ToString(), query);
        return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
    }
}


[HttpGet("log_sistema_filtro")]
public async Task<IActionResult> FiltrarLogSistema(
    [FromHeader(Name = "Authorization")] string token,
    [FromQuery] string? accion,
    [FromQuery] DateTime? fechaInicio,
    [FromQuery] DateTime? fechaFin,
    [FromQuery] string? cambiosRealizados,
    [FromQuery] int? administradorId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var startTime = DateTime.UtcNow;
    string query = "SELECT * FROM log_sistema WHERE 1=1";

   
    if (!await ValidateAdminTokenAsync(token))
    {
        var executionTime = DateTime.UtcNow - startTime;
        return Unauthorized(new { mensaje = "Token no autorizado" });
        
    }

    try
    {
        var queryBase = _context.LogSistema.Include(l => l.Administrador).AsQueryable();

        // Filtrar por acción
        if (!string.IsNullOrEmpty(accion))
        {
            queryBase = queryBase.Where(l => l.log_sistema_accion.Contains(accion));
            query += $" AND log_sistema_accion LIKE '%{accion}%'";
        }

        // Filtrar por fecha de inicio
        if (fechaInicio.HasValue)
        {
            queryBase = queryBase.Where(l => l.log_sistema_fecha >= fechaInicio.Value);
            query += $" AND log_sistema_fecha >= '{fechaInicio.Value:yyyy-MM-dd HH:mm:ss}'";
        }

        // Filtrar por fecha de fin
        if (fechaFin.HasValue)
        {
            queryBase = queryBase.Where(l => l.log_sistema_fecha <= fechaFin.Value);
            query += $" AND log_sistema_fecha <= '{fechaFin.Value:yyyy-MM-dd HH:mm:ss}'";
        }

        // Filtrar por cambios realizados
        if (!string.IsNullOrEmpty(cambiosRealizados))
        {
            queryBase = queryBase.Where(l => l.log_sistema_cambios_realizados.Contains(cambiosRealizados));
            query += $" AND log_sistema_cambios_realizados LIKE '%{cambiosRealizados}%'";
        }

        // Filtrar por administrador_id 
        if (administradorId.HasValue)
        {
            queryBase = queryBase.Where(l => l.administrador_id == administradorId.Value);
            query += $" AND administrador_id = {administradorId.Value}";
        }

        var totalItems = await queryBase.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        // Obtener los resultados filtrados con paginación
        var logs = await queryBase
            .OrderByDescending(l => l.log_sistema_fecha)  
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        query += $" ORDER BY log_sistema_fecha DESC LIMIT {(page - 1) * pageSize}, {pageSize};";

        //respuesta
        var response = new
        {
            totalItems,
            pageSize,
            currentPage = page,
            totalPages,
            data = logs.Select(l => new
            {
                l.log_sistema_id,
                l.log_sistema_accion,
                l.log_sistema_fecha,
                l.log_sistema_cambios_realizados,
                created_at = l.created_at.ToString("yyyy-MM-dd HH:mm:ss"), // Formato de fecha
                administrador = new
                {
                    nombre = l.Administrador.administrador_nombres,
                    apellido = l.Administrador.administrador_apellidos
                }
            })
        };

        var executionTime = DateTime.UtcNow - startTime;

        //logs
        var administradorIda = await ObtenerAdministradorIdDesdeToken(token);
        await CrearLogSistema("Filtrar Log Sistema", administradorIda);

        await CrearLogApi("GET", "/logs/log_sistema_filtro", "200", token, "Archivo csv generado", executionTime.TotalMilliseconds.ToString(), query);

        return Ok(response);
    }
    catch (Exception ex)
    {
        var errorExecutionTime = DateTime.UtcNow - startTime;
        await CrearLogApi("GET", "/logs/log_sistema_filtro", "500", token, "Archivo csv generado", "", query);
        return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
    }
    
}

[HttpGet("exportar/log_sistema")]
public async Task<IActionResult> ExportarLogSistema(
    [FromHeader(Name = "Authorization")] string token,
    [FromQuery] string? accion,
    [FromQuery] DateTime? fechaInicio,
    [FromQuery] DateTime? fechaFin,
    [FromQuery] string? cambiosRealizados)
{
    var startTime = DateTime.UtcNow;
    string query = "SELECT * FROM log_sistema WHERE 1=1";

    
    if (!await ValidateAdminTokenAsync(token))
    {
        var executionTime = DateTime.UtcNow - startTime;
        return Unauthorized(new { mensaje = "Token no autorizado" });
    }

    try
    {
        var queryBase = _context.LogSistema.AsQueryable();

        if (!string.IsNullOrEmpty(accion))
            queryBase = queryBase.Where(l => l.log_sistema_accion.Contains(accion));

        if (fechaInicio.HasValue)
            queryBase = queryBase.Where(l => l.log_sistema_fecha >= fechaInicio.Value);

        if (fechaFin.HasValue)
            queryBase = queryBase.Where(l => l.log_sistema_fecha <= fechaFin.Value);

        if (!string.IsNullOrEmpty(cambiosRealizados))
            queryBase = queryBase.Where(l => l.log_sistema_cambios_realizados.Contains(cambiosRealizados));

        var logs = await queryBase.ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Acción,Fecha,Cambios Realizados,Fecha Creación,Administrador ID");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.log_sistema_id}," +
                          $"\"{log.log_sistema_accion}\"," +
                          $"{log.log_sistema_fecha?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                          $"\"{log.log_sistema_cambios_realizados}\"," +
                          $"{log.created_at}," +
                          $"{log.administrador_id}");
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var executionTime = DateTime.UtcNow - startTime;
        var administradorId = await ObtenerAdministradorIdDesdeToken(token);
        await CrearLogSistema("Exportar Log Sistema", administradorId);

        await CrearLogApi("GET", "/logs/exportar/log_sistema", "200", token, "Archivo csv generado", executionTime.TotalMilliseconds.ToString(), query);

        return File(content, "text/csv", "log_sistema.csv");
    }
    catch (Exception ex)
    {
        var errorExecutionTime = DateTime.UtcNow - startTime;
        return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
    }
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

    return tokenEntry ?? 0; 
}
    }
}