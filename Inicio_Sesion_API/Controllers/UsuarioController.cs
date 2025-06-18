using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;
using ClosedXML.Excel;
using System.Security.Cryptography;



namespace Inicio_Sesion_API.Controllers;

[ApiController]
[Route("usuario")]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public UsuarioController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }


    [HttpGet]
    public async Task<IActionResult> GetUsuarios([FromHeader(Name = "Authorization")] string token, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var stopwatch = Stopwatch.StartNew();
        var tiempoInicio = DateTime.UtcNow;
        string query = string.Empty;


        // Validar el token
        if (!await ValidateAdminTokenAsync(token))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            await CrearLogApi("GET", "/usuario", "401", token, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        try
        {
            var totalItems = await _context.Usuario.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var usuarios = await _context.Usuario
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync() ?? new List<Usuario>();

            if (usuarios == null || usuarios.Count == 0)
            {
                usuarios = await _context.Usuario
                    .Take(pageSize)
                    .ToListAsync();
                page = 1;
            }

            var response = new
            {
                totalItems,
                pageSize,
                currentPage = page,
                totalPages,
                data = usuarios.Select(u => new
                {
                    u.usuario_id,
                    u.usuario_username,
                    u.usuario_nombres,
                    u.usuario_apellidos,
                    u.usuario_email,
                    u.usuario_fono
                })
            };

            stopwatch.Stop();
           
            string responseJson = JsonConvert.SerializeObject(response);

            // Logs
            query = "Select * from usuarios";
            await CrearLogApi("GET", "/usuario", "200", token, responseJson, stopwatch.Elapsed.TotalMilliseconds.ToString(), query);
            
            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Obtener Usuarios", administradorId);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log detallado
            await CrearLogApi("GET", "/usuario", "500", token, ex.ToString(), stopwatch.Elapsed.TotalMilliseconds.ToString(), query);

            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
    }



[HttpPut("edit/{id}")]
public async Task<IActionResult> EditarUsuario(int id, [FromHeader(Name = "Authorization")] string token, [FromBody] Usuario updatedUsuario)
{
    var parametrosEntrada = JsonConvert.SerializeObject(new { token, updatedUsuario });
    var tiempoInicio = DateTime.UtcNow;
    string query = "";

    try
    {
        if (!await ValidateAdminTokenAsync(token))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            await CrearLogApi("PUT", "/usuario/edit/" + id, "401", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        var usuario = await _context.Usuario.FindAsync(id);
        if (usuario == null)
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Usuario no encontrado" });
            await CrearLogApi("PUT", "/usuario/edit/" + id, "404", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return NotFound(new { mensaje = "Usuario no encontrado" });
        }

        // Registrar cambios realizados
        string cambiosRealizados = "";
        if (usuario.usuario_username != updatedUsuario.usuario_username)
            cambiosRealizados += $"Username cambiado de {usuario.usuario_username} a {updatedUsuario.usuario_username}. ";
        if (usuario.usuario_password != updatedUsuario.usuario_password)
            cambiosRealizados += $"Password actualizado. ";
        if (usuario.usuario_nombres != updatedUsuario.usuario_nombres)
            cambiosRealizados += $"Nombres cambiados de {usuario.usuario_nombres} a {updatedUsuario.usuario_nombres}. ";
        if (usuario.usuario_apellidos != updatedUsuario.usuario_apellidos)
            cambiosRealizados += $"Apellidos cambiados de {usuario.usuario_apellidos} a {updatedUsuario.usuario_apellidos}. ";
        if (usuario.usuario_email != updatedUsuario.usuario_email)
            cambiosRealizados += $"Email cambiado de {usuario.usuario_email} a {updatedUsuario.usuario_email}. ";

        // Actualizar los campos, reemplazando null con ""
        usuario.usuario_username = updatedUsuario.usuario_username ?? "";
        
        // Aplicar SHA-256 
        if (!string.IsNullOrEmpty(updatedUsuario.usuario_password)) 
        {
            usuario.usuario_password = HashPassword(updatedUsuario.usuario_password); 
        }
        
        usuario.usuario_nombres = updatedUsuario.usuario_nombres ?? "";
        usuario.usuario_apellidos = updatedUsuario.usuario_apellidos ?? "";
        usuario.usuario_rut = updatedUsuario.usuario_rut ?? "";
        usuario.usuario_email = updatedUsuario.usuario_email ?? "";
        usuario.usuario_fono = updatedUsuario.usuario_fono ?? "";
        usuario.usuario_url = updatedUsuario.usuario_url ?? "";
        usuario.usuario_sistema = updatedUsuario.usuario_sistema ?? "";
        usuario.updated_at = DateTime.UtcNow;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _context.Usuario.Update(usuario);
        await _context.SaveChangesAsync();
        stopwatch.Stop();

        query = $"UPDATE usuario SET " +
        $"`usuario_password` = '{usuario.usuario_password}', " +
        $"`usuario_nombres` = '{usuario.usuario_nombres}', " +
        $"`usuario_apellidos` = '{usuario.usuario_apellidos}', " +
        $"`usuario_rut` = '{usuario.usuario_rut}', " +
        $"`usuario_email` = '{usuario.usuario_email}', " +
        $"`usuario_fono` = '{usuario.usuario_fono}', " +
        $"`usuario_url` = '{usuario.usuario_url}' " +
        $"WHERE `usuario_username` = '{usuario.usuario_username}';";

        var parametrosSalidaExito = JsonConvert.SerializeObject(new { mensaje = "Usuario actualizado exitosamente" });
        await CrearLogApi("PUT", "/usuario/edit/" + id, "200", parametrosEntrada, parametrosSalidaExito, stopwatch.ElapsedMilliseconds.ToString(), query);

        var administradorId = await ObtenerAdministradorIdDesdeToken(token);
        await CrearLogSistema("Editar Usuario", administradorId, cambiosRealizados);
       
        return Ok(new { mensaje = "Usuario actualizado exitosamente" });
    }
    catch (Exception ex)
    {
        var parametrosSalidaError = JsonConvert.SerializeObject(new { mensaje = "Error interno del servidor.", error = ex.Message });
        await CrearLogApi("PUT", "/usuario/edit/" + id, "500", parametrosEntrada, parametrosSalidaError, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), ex.Message);

        return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
    }
}


    public bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        return emailRegex.IsMatch(email);
    }


    [HttpGet("filtrar")]
    public async Task<IActionResult> FiltrarUsuarios(
        [FromHeader(Name = "Authorization")] string token,
        [FromQuery] string? nombres,
        [FromQuery] string? apellidos,
        [FromQuery] string? email,
        [FromQuery] string? fono,
        [FromQuery] DateTime? fechaInicio,   
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var startTime = DateTime.UtcNow;
        string query = "SELECT usuario_id, usuario_username, usuario_nombres, usuario_apellidos, usuario_email, usuario_fono FROM usuario WHERE 1=1"; // Consulta base

       
        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/usuario/filtrar", "401", token, "Token no autorizado o no pertenece a un administrador", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        try
        {
            
            var queryBase = _context.Usuario.AsQueryable();

            // Filtrar por nombres
            if (!string.IsNullOrEmpty(nombres))
            {
                queryBase = queryBase.Where(u => u.usuario_nombres.Contains(nombres));
                query += $" AND usuario_nombres LIKE '%{nombres}%'";
            }

            // Filtrar por apellidos
            if (!string.IsNullOrEmpty(apellidos))
            {
                queryBase = queryBase.Where(u => u.usuario_apellidos.Contains(apellidos));
                query += $" AND usuario_apellidos LIKE '%{apellidos}%'";
            }

            // Filtrar por correo electrónico
            if (!string.IsNullOrEmpty(email))
            {
                queryBase = queryBase.Where(u => u.usuario_email.Contains(email));
                query += $" AND usuario_email LIKE '%{email}%'";
            }

            // Filtrar por teléfono
            if (!string.IsNullOrEmpty(fono))
            {
                queryBase = queryBase.Where(u => u.usuario_fono.Contains(fono));
                query += $" AND usuario_fono LIKE '%{fono}%'";
            }

            // Filtrar por fecha de inicio
            if (fechaInicio.HasValue)
            {
                queryBase = queryBase.Where(u => u.created_at >= fechaInicio.Value);
                query += $" AND created_at >= '{fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Filtrar por fecha de fin
            if (fechaFin.HasValue)
            {
                queryBase = queryBase.Where(u => u.created_at <= fechaFin.Value);
                query += $" AND created_at <= '{fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }



            // Calcular el total de registros que coinciden con los filtros
            var totalItems = await queryBase.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Obtener los usuarios filtrados con paginación
            var usuarios = await queryBase
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Si no se encuentran usuarios,caso vacío
            if (usuarios == null || usuarios.Count == 0)
            {
                usuarios = await queryBase
                    .Take(pageSize)
                    .ToListAsync();
                page = 1;
            }

            // Agregar la paginación al query SQL (para log_api)
            query += $" LIMIT {(page - 1) * pageSize}, {pageSize};";

            // Respuesta
            var response = new
            {
                totalItems,
                pageSize,
                currentPage = page,
                totalPages,
                data = usuarios.Select(u => new
                {
                    u.usuario_id,
                    u.usuario_username,
                    u.usuario_nombres,
                    u.usuario_apellidos,
                    u.usuario_email,
                    u.usuario_fono,
                    u.created_at
                })
            };

           
            var executionTime = DateTime.UtcNow - startTime;

            //Logs
            await CrearLogApi("GET", "/usuario/filtrar", "200", token, JsonConvert.SerializeObject(response), executionTime.TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Filtrar Usuarios", administradorId);
            return Ok(response);
        }
        catch (Exception ex)
        {
       
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/usuario/filtrar", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> BorrarUsuario(int id, [FromHeader(Name = "Authorization")] string token)
    {
        
        var parametrosEntrada = JsonConvert.SerializeObject(new { token });
        var tiempoInicio = DateTime.UtcNow;
        string query = "";

        try
        {
          
            if (!await ValidateAdminTokenAsync(token))
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
                await CrearLogApi("DELETE", "/usuario/delete/" + id, "401", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            }

           
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.usuario_id == id);



            if (usuario == null)
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Usuario no encontrado" });
                await CrearLogApi("DELETE", "/usuario/delete/" + id, "404", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return NotFound(new { mensaje = "Usuario no encontrado" });
            }

            // Eliminar el usuario
            _context.Usuario.Remove(usuario);
            await _context.SaveChangesAsync();

            // logs
            var parametrosSalidaExito = JsonConvert.SerializeObject(new { mensaje = "Usuario eliminado exitosamente" });
            query = $"DELETE from usuario where usuario_id={usuario.usuario_id}";
            await CrearLogApi("DELETE", "/usuario/delete/" + id, "200", parametrosEntrada, parametrosSalidaExito, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Eliminar Usuario", administradorId);

            return Ok(new { mensaje = "Usuario eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            var parametrosSalidaError = JsonConvert.SerializeObject(new { mensaje = "Error interno del servidor. Inténtelo nuevamente más tarde.", error = ex.Message });
            await CrearLogApi("DELETE", "/usuario/delete/" + id, "500", parametrosEntrada, parametrosSalidaError, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), ex.Message);

            return StatusCode(500, new { mensaje = "Error interno del servidor. Inténtelo nuevamente más tarde.", error = ex.Message });
        }
    }




    [HttpGet("exportar/usuarios")]
    public async Task<IActionResult> ExportarUsuarios(
        [FromHeader(Name = "Authorization")] string token,
        [FromQuery] string? nombres,
        [FromQuery] string? apellidos,
        [FromQuery] string? email,
        [FromQuery] string? fono,
        [FromQuery] DateTime? fechaInicio, 
        [FromQuery] DateTime? fechaFin)
    {
        var startTime = DateTime.UtcNow;
        string query = "SELECT usuario_id, usuario_username, usuario_nombres, usuario_apellidos, usuario_email, usuario_fono FROM usuario WHERE 1=1";

        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/usuario/exportar/usuarios", "401", token, "Token no autorizado", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado" });
        }

        try
        {
            var queryBase = _context.Usuario.AsQueryable();

            
            if (!string.IsNullOrEmpty(nombres))
            {
                queryBase = queryBase.Where(u => u.usuario_nombres.Contains(nombres));
            }
            if (!string.IsNullOrEmpty(apellidos))
            {
                queryBase = queryBase.Where(u => u.usuario_apellidos.Contains(apellidos));
            }
            if (!string.IsNullOrEmpty(email))
            {
                queryBase = queryBase.Where(u => u.usuario_email.Contains(email));
            }
            if (!string.IsNullOrEmpty(fono))
            {
                queryBase = queryBase.Where(u => u.usuario_fono.Contains(fono));
            }
            
            if (fechaInicio.HasValue)
            {
                queryBase = queryBase.Where(u => u.created_at >= fechaInicio.Value);
                query += $" AND created_at >= '{fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            if (fechaFin.HasValue)
            {
                queryBase = queryBase.Where(u => u.created_at <= fechaFin.Value);
                query += $" AND created_at <= '{fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Obtener todos los usuarios si no hay filtros
            var usuarios = await queryBase.ToListAsync();

            // Si no hay usuarios, devuelve un archivo vacío
            if (usuarios == null || !usuarios.Any())
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Usuarios");
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Username";
                    worksheet.Cell(1, 3).Value = "Nombres";
                    worksheet.Cell(1, 4).Value = "Apellidos";
                    worksheet.Cell(1, 5).Value = "Email";
                    worksheet.Cell(1, 6).Value = "Fono";
                    worksheet.Cell(1, 7).Value = "Fecha de Creación";
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "usuarios_vacios.xlsx");
                    }
                }
            }

            // Si hay usuarios, generar el archivo Excel con los datos
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Usuarios");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Username";
                worksheet.Cell(1, 3).Value = "Nombres";
                worksheet.Cell(1, 4).Value = "Apellidos";
                worksheet.Cell(1, 5).Value = "Email";
                worksheet.Cell(1, 6).Value = "Fono";
                worksheet.Cell(1, 7).Value = "Fecha de Creación";

                int row = 2;
                foreach (var u in usuarios)
                {
                    worksheet.Cell(row, 1).Value = u.usuario_id;
                    worksheet.Cell(row, 2).Value = u.usuario_username;
                    worksheet.Cell(row, 3).Value = u.usuario_nombres ?? "";
                    worksheet.Cell(row, 4).Value = u.usuario_apellidos ?? "";
                    worksheet.Cell(row, 5).Value = u.usuario_email ?? "";
                    worksheet.Cell(row, 6).Value = u.usuario_fono ?? "";
                    worksheet.Cell(row, 7).Value = u.created_at.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    var executionTime = DateTime.UtcNow - startTime;
                    await CrearLogApi("GET", "/usuario/exportar/usuarios", "200", token, "Archivo generado", executionTime.TotalMilliseconds.ToString(), query);

                    var administradorId = await ObtenerAdministradorIdDesdeToken(token);
                    await CrearLogSistema("Exportar Usuarios", administradorId,"Archivo Creado");
                    
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "usuarios.xlsx");
                }
            }
        }
        catch (Exception ex)
        {
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/usuario/exportar/usuarios", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
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

        var tokenEntry = await _context.Token
            .FirstOrDefaultAsync(t => t.token == token && t.administrador_id != null);

      
        if (tokenEntry == null)
        {
            return false;
        }

        return true;
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
private string HashPassword(string password)
{
    using (SHA256 sha256Hash = SHA256.Create())
    {
        // Convertir la contraseña a un array de bytes y calcular el hash
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

        // Convertir el hash a una cadena hexadecimal
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
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

    //CREAR LOG SISTEMA
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
