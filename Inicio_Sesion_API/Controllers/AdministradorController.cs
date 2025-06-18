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
[Route("administrador")]
public class AdministradorController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AdministradorController(AppDbContext context, IConfiguration configuration)
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



    // GET: /administrador
    [HttpGet]
    public async Task<IActionResult> GetAdministradores([FromHeader(Name = "Authorization")] string token, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var stopwatch = Stopwatch.StartNew();
        var tiempoInicio = DateTime.UtcNow;
        string query = "";

        // Validar el token y verificar que pertenece a un administrador
        if (!await ValidateAdminTokenAsync(token))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            await CrearLogApi("GET", "/administrador", "401", token, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        try
        {
            var totalItems = await _context.Administrador.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var administradores = await _context.Administrador
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync() ?? new List<Administrador>();

            if (administradores == null || administradores.Count == 0)
            {
                administradores = await _context.Administrador
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
                data = administradores.Select(a => new
                {
                    a.administrador_id,
                    a.administrador_nombres,
                    a.administrador_apellidos,
                    a.administrador_email,
                    a.administrador_fono,
                    created_at = a.created_at.ToString("yyyy-MM-dd HH:mm:ss") // Formato de fecha
                })
            };

            stopwatch.Stop();
            string responseJson = JsonConvert.SerializeObject(response);

            query = "SELECT * FROM administrador LIMIT @pageSize OFFSET @offset;";
            await CrearLogApi("GET", "/administrador", "200", token, responseJson, stopwatch.Elapsed.TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Obtener Administrador", administradorId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await CrearLogApi("GET", "/administrador", "500", token, ex.ToString(), stopwatch.Elapsed.TotalMilliseconds.ToString(), query);

            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("edit/{id}")]
    public async Task<IActionResult> EditarAdministrador(int id, [FromHeader(Name = "Authorization")] string token, [FromBody] Administrador updatedAdministrador)
    {
        var parametrosEntrada = JsonConvert.SerializeObject(new { token, updatedAdministrador });
        var tiempoInicio = DateTime.UtcNow;
        string query = "";

        try
        {
            if (!await ValidateAdminTokenAsync(token))
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
                await CrearLogApi("PUT", "/administrador/edit/" + id, "401", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            }

            var administrador = await _context.Administrador.FindAsync(id);
            if (administrador == null)
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Administrador no encontrado" });
                await CrearLogApi("PUT", "/administrador/edit/" + id, "404", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return NotFound(new { mensaje = "Administrador no encontrado" });
            }
            // Registrar cambios realizados
            string cambiosRealizados = "";
            if (administrador.administrador_nombres != updatedAdministrador.administrador_nombres)
                cambiosRealizados += $"Nombres cambiado de {administrador.administrador_nombres} a {updatedAdministrador.administrador_nombres}. ";
            if (administrador.administrador_apellidos != updatedAdministrador.administrador_apellidos)
                cambiosRealizados += $"Apellidos cambiados de {administrador.administrador_apellidos} a {updatedAdministrador.administrador_apellidos}. ";
            if (administrador.administrador_email != updatedAdministrador.administrador_email)
                cambiosRealizados += $"Email cambiados de {administrador.administrador_email} a {updatedAdministrador.administrador_email}. ";
            if (administrador.administrador_fono != updatedAdministrador.administrador_fono)
                cambiosRealizados += $"Fono cambiado de {administrador.administrador_fono} a {updatedAdministrador.administrador_fono}. ";
            // Actualizar los campos, reemplazando null con valores actuales
            administrador.administrador_nombres = updatedAdministrador.administrador_nombres ?? "";
            administrador.administrador_apellidos = updatedAdministrador.administrador_apellidos ?? "";
            administrador.administrador_email = updatedAdministrador.administrador_email ?? "";
            administrador.administrador_fono = updatedAdministrador.administrador_fono ?? "";
            administrador.updated_at = DateTime.UtcNow;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _context.Administrador.Update(administrador);
            await _context.SaveChangesAsync();
            stopwatch.Stop();

            query = $"UPDATE administrador SET " +
            $"`administrador_nombres` = '{administrador.administrador_nombres}', " +
            $"`administrador_apellidos` = '{administrador.administrador_apellidos}', " +
            $"`administrador_email` = '{administrador.administrador_email}', " +
            $"`administrador_fono` = '{administrador.administrador_fono}', " +
            $"`updated_at` = '{administrador.updated_at:yyyy-MM-dd HH:mm:ss}' " +
            $"WHERE `administrador_id` = {id};";

            var parametrosSalidaExito = JsonConvert.SerializeObject(new { mensaje = "Administrador actualizado exitosamente" });
            await CrearLogApi("PUT", "/administrador/edit/" + id, "200", parametrosEntrada, parametrosSalidaExito, stopwatch.ElapsedMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Editar Administrador", administradorId, cambiosRealizados);

            return Ok(new { mensaje = "Administrador actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            var parametrosSalidaError = JsonConvert.SerializeObject(new { mensaje = "Error interno del servidor.", error = ex.Message });
            await CrearLogApi("PUT", "/administrador/edit/" + id, "500", parametrosEntrada, parametrosSalidaError, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), ex.Message);

            return StatusCode(500, new { mensaje = "Error interno del servidor.", error = ex.Message });
        }
    }



    [HttpGet("filtrar")]
    public async Task<IActionResult> FiltrarAdministradores(
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
        string query = "SELECT admin_id, admin_nombres, admin_apellidos, admin_email, admin_fono FROM administrador WHERE 1=1"; // Consulta base

        
        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/administrador/filtrar", "401", token, "Token no autorizado o no pertenece a un administrador", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
        }

        try
        {
            
            var queryBase = _context.Administrador.AsQueryable();

            // Filtrar por nombres
            if (!string.IsNullOrEmpty(nombres))
            {
                queryBase = queryBase.Where(a => a.administrador_nombres.Contains(nombres));
                query += $" AND admin_nombres LIKE '%{nombres}%'";
            }

            // Filtrar por apellidos
            if (!string.IsNullOrEmpty(apellidos))
            {
                queryBase = queryBase.Where(a => a.administrador_apellidos.Contains(apellidos));
                query += $" AND admin_apellidos LIKE '%{apellidos}%'";
            }

            // Filtrar por email
            if (!string.IsNullOrEmpty(email))
            {
                queryBase = queryBase.Where(a => a.administrador_email.Contains(email));
                query += $" AND admin_email LIKE '%{email}%'";
            }

            // Filtrar por teléfono
            if (!string.IsNullOrEmpty(fono))
            {
                queryBase = queryBase.Where(a => a.administrador_fono.Contains(fono));
                query += $" AND admin_fono LIKE '%{fono}%'";
            }

            // Filtrar por fecha de inicio
            if (fechaInicio.HasValue)
            {
                queryBase = queryBase.Where(a => a.created_at >= fechaInicio.Value);
                query += $" AND created_at >= '{fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Filtrar por fecha de fin
            if (fechaFin.HasValue)
            {
                queryBase = queryBase.Where(a => a.created_at <= fechaFin.Value);
                query += $" AND created_at <= '{fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Calcular el total de registros filtrados
            var totalItems = await queryBase.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // paginación
            var administradores = await queryBase
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Si no hay resultados, restablecer paginación
            if (administradores == null || administradores.Count == 0)
            {
                administradores = await queryBase.Take(pageSize).ToListAsync();
                page = 1;
            }

            // Agregar paginación al query SQL (para log_api)
            query += $" LIMIT {(page - 1) * pageSize}, {pageSize};";

            // respuesta
            var response = new
            {
                totalItems,
                pageSize,
                currentPage = page,
                totalPages,
                data = administradores.Select(a => new
                {
                    a.administrador_id,
                    a.administrador_nombres,
                    a.administrador_apellidos,
                    a.administrador_email,
                    a.administrador_fono,
                    a.created_at
                })
            };

            //tiempo de ejecución
            var executionTime = DateTime.UtcNow - startTime;

            // Crear el log
            await CrearLogApi("GET", "/administrador/filtrar", "200", token, JsonConvert.SerializeObject(response), executionTime.TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Filtrar Administrador", administradorId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Manejo de errores
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/administrador/filtrar", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }

    [HttpGet("exportar/administradores")]
    public async Task<IActionResult> ExportarAdministradores(
        [FromHeader(Name = "Authorization")] string token,
        [FromQuery] string? nombres,
        [FromQuery] string? apellidos,
        [FromQuery] string? email,
        [FromQuery] string? fono,
        [FromQuery] DateTime? fechaInicio, 
        [FromQuery] DateTime? fechaFin)
    {
        var startTime = DateTime.UtcNow;
        string query = "SELECT administrador_id, administrador_nombres, administrador_apellidos, administrador_email, administrador_fono FROM administrador WHERE 1=1";

        
        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/administrador/exportar/administradores", "401", token, "Token no autorizado", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado" });
        }

        try
        {
            var queryBase = _context.Administrador.AsQueryable();

            if (!string.IsNullOrEmpty(nombres))
            {
                queryBase = queryBase.Where(a => a.administrador_nombres.Contains(nombres));
            }
            if (!string.IsNullOrEmpty(apellidos))
            {
                queryBase = queryBase.Where(a => a.administrador_apellidos.Contains(apellidos));
            }
            if (!string.IsNullOrEmpty(email))
            {
                queryBase = queryBase.Where(a => a.administrador_email.Contains(email));
            }
            if (!string.IsNullOrEmpty(fono))
            {
                queryBase = queryBase.Where(a => a.administrador_fono.Contains(fono));
            }
            // Filtrar por fecha de inicio, si se proporciona el parámetro
            if (fechaInicio.HasValue)
            {
                queryBase = queryBase.Where(a => a.created_at >= fechaInicio.Value);
                query += $" AND created_at >= '{fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Filtrar por fecha de fin, si se proporciona el parámetro
            if (fechaFin.HasValue)
            {
                queryBase = queryBase.Where(a => a.created_at <= fechaFin.Value);
                query += $" AND created_at <= '{fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }

            // Obtener todos los administradores si no hay filtros
            var administradores = await queryBase.ToListAsync();

            // Si no hay administradores, devuelve un archivo vacío
            if (administradores == null || !administradores.Any())
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Administradores");
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Nombres";
                    worksheet.Cell(1, 3).Value = "Apellidos";
                    worksheet.Cell(1, 4).Value = "Email";
                    worksheet.Cell(1, 5).Value = "Fono";
                    worksheet.Cell(1, 6).Value = "Fecha de Creación";
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "administradores_vacios.xlsx");
                    }
                }
            }

            // Si hay administradores, generar el archivo Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Administradores");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Nombres";
                worksheet.Cell(1, 3).Value = "Apellidos";
                worksheet.Cell(1, 4).Value = "Email";
                worksheet.Cell(1, 5).Value = "Fono";
                worksheet.Cell(1, 6).Value = "Fecha de Creación";

                int row = 2;
                foreach (var a in administradores)
                {
                    worksheet.Cell(row, 1).Value = a.administrador_id;
                    worksheet.Cell(row, 2).Value = a.administrador_nombres ?? "";
                    worksheet.Cell(row, 3).Value = a.administrador_apellidos ?? "";
                    worksheet.Cell(row, 4).Value = a.administrador_email ?? "";
                    worksheet.Cell(row, 5).Value = a.administrador_fono ?? "";
                    worksheet.Cell(row, 6).Value = a.created_at.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    var executionTime = DateTime.UtcNow - startTime;
                    await CrearLogApi("GET", "/administrador/exportar/administradores", "200", token, "Archivo generado", executionTime.TotalMilliseconds.ToString(), query);

                    var administradorId = await ObtenerAdministradorIdDesdeToken(token);
                    await CrearLogSistema("Exportar Administradores", administradorId);

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "administradores.xlsx");
                }
            }
        }
        catch (Exception ex)
        {
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("GET", "/administrador/exportar/administradores", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> BorrarAdministrador(int id, [FromHeader(Name = "Authorization")] string token)
    {
        // Registrar el inicio del log
        var parametrosEntrada = JsonConvert.SerializeObject(new { token });
        var tiempoInicio = DateTime.UtcNow;
        string query = "";

        try
        {
            
            if (!await ValidateAdminTokenAsync(token))
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
                await CrearLogApi("DELETE", "/administrador/delete/" + id, "401", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return Unauthorized(new { mensaje = "Token no autorizado o no pertenece a un administrador" });
            }

            // Buscar el administrador en la base de datos
            var administrador = await _context.Administrador.FirstOrDefaultAsync(a => a.administrador_id == id);

            if (administrador == null)
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Administrador no encontrado" });
                await CrearLogApi("DELETE", "/administrador/delete/" + id, "404", parametrosEntrada, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return NotFound(new { mensaje = "Administrador no encontrado" });
            }

            // Eliminar el administrador
            _context.Administrador.Remove(administrador);
            await _context.SaveChangesAsync();

            // logs
            var parametrosSalidaExito = JsonConvert.SerializeObject(new { mensaje = "Administrador eliminado exitosamente" });
            query = $"DELETE from administrador where administrador_id={administrador.administrador_id}";
            await CrearLogApi("DELETE", "/administrador/delete/" + id, "200", parametrosEntrada, parametrosSalidaExito, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Eliminar Administrador", administradorId);

            return Ok(new { mensaje = "Administrador eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            var parametrosSalidaError = JsonConvert.SerializeObject(new { mensaje = "Error interno del servidor. Inténtelo nuevamente más tarde.", error = ex.Message });
            await CrearLogApi("DELETE", "/administrador/delete/" + id, "500", parametrosEntrada, parametrosSalidaError, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), ex.Message);

            return StatusCode(500, new { mensaje = "Error interno del servidor. Inténtelo nuevamente más tarde.", error = ex.Message });
        }
    }

    [HttpPost("crear-administrador")]
    public async Task<IActionResult> CrearAdministrador(
        [FromHeader(Name = "Authorization")] string token,
        [FromBody] Administrador nuevoAdministrador)
    {
        var startTime = DateTime.UtcNow;
        string query = "INSERT INTO administrador (administrador_email, administrador_password, administrador_nombres, administrador_apellidos, administrador_fono) VALUES (@Email, @Password, @Nombres, @Apellidos, @Fono)";

       
        if (!await ValidateAdminTokenAsync(token))
        {
            var executionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("POST", "/administrador", "401", token, "Token no autorizado", executionTime.TotalMilliseconds.ToString(), query);
            return Unauthorized(new { mensaje = "Token no autorizado" });
        }

        try
        {
            // Validación de los datos del nuevo administrador
            if (string.IsNullOrEmpty(nuevoAdministrador.administrador_nombres))
            {
                return BadRequest(new { mensaje = "El nombre es obligatorio." });
            }
            if (string.IsNullOrEmpty(nuevoAdministrador.administrador_email))
            {
                return BadRequest(new { mensaje = "El email es obligatorio." });
            }
            if (string.IsNullOrEmpty(nuevoAdministrador.administrador_password))
            {
                return BadRequest(new { mensaje = "La contraseña es obligatoria." });
            }

            
            _context.Administrador.Add(nuevoAdministrador);
            await _context.SaveChangesAsync();

            var executionTimePost = DateTime.UtcNow - startTime;
            await CrearLogApi("POST", "/administrador", "201", token, "Administrador creado", executionTimePost.TotalMilliseconds.ToString(), query);

            var administradorId = await ObtenerAdministradorIdDesdeToken(token);
            await CrearLogSistema("Agregar Administrador", administradorId);

            return CreatedAtRoute("GetAdministrador", new { id = nuevoAdministrador.administrador_id }, nuevoAdministrador);
        }
        catch (Exception ex)
        {
            var errorExecutionTime = DateTime.UtcNow - startTime;
            await CrearLogApi("POST", "/administrador", "500", token, ex.Message, errorExecutionTime.TotalMilliseconds.ToString(), query);
            return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
        }
    }

    [HttpPost]
    [Route("cerrar_sesion_user")]
    public async Task<IActionResult> CerrarSesionUsuario(
        [FromHeader(Name = "Authorization")] string tokenAdmin,
        [FromQuery] int usuarioId)
    {
        var tiempoInicio = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        
        if (!await ValidateAdminTokenAsync(tokenAdmin))
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token de administrador no autorizado" });
            await CrearLogApi("POST", "/administrador/cerrar_sesion_user", "401", tokenAdmin, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token de administrador no autorizado" });
        }

        try
        {
            // Buscar el token del usuario en la base de datos
            var tokensUsuario = await _context.Token.Where(t => t.usuario_id == usuarioId).ToListAsync();

            if (tokensUsuario == null || tokensUsuario.Count == 0)
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "El usuario no tiene sesiones activas" });
                await CrearLogApi("POST", "/administrador/cerrar_sesion_user", "404", tokenAdmin, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return NotFound(new { mensaje = "El usuario no tiene sesiones activas" });
            }

            // Eliminar los tokens encontrados
            _context.Token.RemoveRange(tokensUsuario);
            await _context.SaveChangesAsync();

            var response = new { mensaje = "Sesión cerrada correctamente" };
            string responseJson = JsonConvert.SerializeObject(response);

            await CrearLogApi("POST", "/administrador/cerrar_sesion_user", "200", tokenAdmin, responseJson, stopwatch.Elapsed.TotalMilliseconds.ToString(), "");

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await CrearLogApi("POST", "/administrador/cerrar_sesion_user", "500", tokenAdmin, ex.ToString(), stopwatch.Elapsed.TotalMilliseconds.ToString(), "");
            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    [Route("cerrar-sesion")]
    public async Task<IActionResult> CerrarSesionAdministrador(
        [FromHeader(Name = "Authorization")] string tokenAdmin)
    {
        var tiempoInicio = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Obtener el ID del administrador desde el token
        var administradorId = await ObtenerAdministradorIdDesdeToken(tokenAdmin);
        if (administradorId == null)
        {
            var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "Token de administrador no autorizado" });
            await CrearLogApi("POST", "/administrador/cerrar-sesion", "401", tokenAdmin, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
            return Unauthorized(new { mensaje = "Token de administrador no autorizado" });
        }

        try
        {
            // Buscar todas las sesiones activas del administrador en la base de datos
            var tokens = await _context.Token.Where(t => t.administrador_id == administradorId).ToListAsync();

            if (tokens == null || tokens.Count == 0)
            {
                var parametrosSalida = JsonConvert.SerializeObject(new { mensaje = "No hay sesiones activas para este administrador" });
                await CrearLogApi("POST", "/administrador/cerrar-sesion", "404", tokenAdmin, parametrosSalida, (DateTime.UtcNow - tiempoInicio).TotalMilliseconds.ToString(), "");
                return NotFound(new { mensaje = "No hay sesiones activas para este administrador" });
            }

            // Eliminar todas las sesiones del administrador
            _context.Token.RemoveRange(tokens);
            await _context.SaveChangesAsync();

            var response = new { mensaje = "Todas las sesiones de administrador han sido cerradas correctamente" };
            string responseJson = JsonConvert.SerializeObject(response);

            await CrearLogApi("POST", "/administrador/cerrar-sesion", "200", tokenAdmin, responseJson, stopwatch.Elapsed.TotalMilliseconds.ToString(), "");

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await CrearLogApi("POST", "/administrador/cerrar-sesion", "500", tokenAdmin, ex.ToString(), stopwatch.Elapsed.TotalMilliseconds.ToString(), "");
            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
    }


    [HttpGet("{id}", Name = "GetAdministrador")]
    public async Task<IActionResult> GetAdministrador(int id)
    {
        var administrador = await _context.Administrador.FindAsync(id);
        if (administrador == null)
        {
            return NotFound();
        }
        return Ok(administrador);
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
    private async Task<bool> ValidateAdminTokenAsync(string token)
    {
        if (!ValidateTokenFormat(token))
            return false;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring(7);

        var tokenEntry = await _context.Token.FirstOrDefaultAsync(t => t.token == token && t.administrador_id != null);
        return tokenEntry != null;
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
