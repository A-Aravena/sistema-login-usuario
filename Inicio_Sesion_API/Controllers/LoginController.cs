using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace Inicio_Sesion_API.Controllers
{
    [ApiController]
    [Route("inicio_sesion")]
    public class InicioSesionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public InicioSesionController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: /inicio_sesion/administrador
        [HttpPost("administrador")]
        public async Task<IActionResult> LoginAdministrador([FromBody] LoginAdminRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { mensaje = "Solicitud inválida. Se requieren email y contraseña." });
                }

                var administrador = await _context.Administrador
                    .FirstOrDefaultAsync(a => a.administrador_email == request.Email);

                if (administrador == null)
                {
                    await RegistrarLog("LoginAdministrador", request.Email, "Fallo: Usuario no encontrado", false, null, null);
                    return Unauthorized(new { mensaje = "Credenciales inválidas." });
                }

                // Convertir la contraseña ingresada a SHA-256 
                string hashedInputPassword = HashPassword(request.Password);

                // Comparar contraseña
                if (!SecureCompare(administrador.administrador_password, hashedInputPassword))
                {
                    await RegistrarLog("LoginAdministrador", request.Email, "Fallo: Contraseña incorrecta", false, null, null);
                    return Unauthorized(new { mensaje = "Credenciales inválidas." });
                }

                var token = GenerateToken(administrador);

                var tokenRecord = new Token
                {
                    token = token,
                    token_login_type = "login_admin",
                    token_expires_at = DateTime.UtcNow.AddHours(30),
                    administrador_id = administrador.administrador_id
                };
                _context.Token.Add(tokenRecord);
                await _context.SaveChangesAsync();

                await RegistrarLog("LoginAdministrador", request.Email, token, true, null, null);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                await RegistrarLog("LoginAdministrador", request.Email, ex.Message, false, null, null);
                return StatusCode(500, new { mensaje = "Error interno del servidor." });
            }
        }
        // POST: /inicio_sesion/usuario
        [HttpPost("usuario")]
        public async Task<IActionResult> LoginUsuario([FromBody] LoginUserRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Sistema))
                {
                    return BadRequest(new { mensaje = "Solicitud inválida. Se requieren username, contraseña y sistema." });
                }

                var sistemasValidos = new[] { "bioemach_autoconsulta", "biomemach_administracion" };
                if (!sistemasValidos.Contains(request.Sistema))
                {
                    await RegistrarLog("LoginUsuario", request.Username, "El sistema proporcionado no es válido.", false, null, null);
                    return Unauthorized(new { mensaje = "El sistema proporcionado no es válido." });
                }

                var usuario = await _context.Usuario
                    .FirstOrDefaultAsync(u => u.usuario_username == request.Username && u.usuario_sistema == request.Sistema);

                if (usuario == null)
                {
                    await RegistrarLog("LoginUsuario", request.Username, "Credenciales inválidas.", false, null, null);
                    return Unauthorized(new { mensaje = "Credenciales inválidas." });
                }

                // Convertir la contraseña ingresada a SHA-256 
                string hashedInputPassword = HashPassword(request.Password);

                // Comparar contraseña
                if (!SecureCompare(usuario.usuario_password, hashedInputPassword))
                {
                    await RegistrarLog("LoginUsuario", request.Username, "Credenciales inválidas.", false, null, null);
                    return Unauthorized(new { mensaje = "Credenciales inválidas." });
                }

                var token = GenerateToken(usuario);

                var tokenRecord = new Token
                {
                    token = token,
                    token_login_type = "login_user",
                    token_expires_at = DateTime.UtcNow.AddHours(30),
                    usuario_id = usuario.usuario_id
                };

                _context.Token.Add(tokenRecord);
                await _context.SaveChangesAsync();

                await RegistrarLog("LoginUsuario", request.Username, request.Sistema, true, usuario.usuario_url, token);
                return Ok(new { token, usuario_url = usuario.usuario_url });
            }
            catch (Exception ex)
            {
                await RegistrarLog("LoginUsuario", request.Username, ex.Message, false, null, null);
                return StatusCode(500, new { mensaje = "Error interno del servidor." });
            }
        }

        private async Task RegistrarLog(string metodo, string identificador, string mensaje, bool exito, string? usuarioUrl, string? token)
        {
            var configuracion = await _context.Configuracion.FirstOrDefaultAsync();

            if (configuracion?.configuracion_log_enable == true)
            {
                var startTime = DateTime.UtcNow;

                string url = Request?.Path.ToString();

                string tipoRespuesta = exito ? "200" : "400"; // Ajusta si necesitas un código de respuesta más específico


                string parametrosEntrada = string.Empty;
                string parametrosSalida = string.Empty;


                string query = "";
                if (metodo == "LoginAdministrador")
                {
                    parametrosEntrada = identificador; 
                    parametrosSalida = mensaje; 

                   
                    if (configuracion.configuracion_log_enable && configuracion.configuracion_nivel_log_api == 1)
                    {
                        query = $"select a.administrador_email, a.administrador_password from administrador a where a.administrador_email = '{identificador}' and a.administrador_password = '{mensaje}';";
                    }
                }
                else if (metodo == "LoginUsuario")
                {
                    parametrosEntrada = $"{identificador} / {mensaje}"; 
                    parametrosSalida = $"{usuarioUrl}/{token}"; 
                    if (parametrosSalida == "/")
                    {
                        parametrosSalida = "Credenciales Erroneas";
                    }

                    
                    if (configuracion.configuracion_log_enable && configuracion.configuracion_nivel_log_api == 1)
                    {
                        query = $" select u.usuario_username ,u.usuario_password , u.usuario_sistema from usuario u  where u.usuario_username= '{identificador}'  and u.usuario_password= '{mensaje}';";
                    }
                }

                //  tiempo de ejecución
                var endTime = DateTime.UtcNow;
                string tiempoEjecucion = (endTime - startTime).TotalMilliseconds.ToString();

                // logs
                var log = new LogApi
                {
                    log_api_metodo = "POST",  // POST para ambos métodos
                    log_api_url = url,  // Acción correspondiente: 'iniciar sesion usuario' o 'iniciar sesion administrador'
                    log_api_tipo_respuesta = tipoRespuesta,  // 200 si éxito, 400 si error
                    log_api_fecha = DateTime.UtcNow,  // Fecha y hora actual
                    log_api_parametros_entrada = parametrosEntrada,  // Email (admin) o username/sistema (usuario)
                    log_api_parametros_salida = parametrosSalida,  // Token (admin) o token/url (usuario)
                    log_api_tiempo_ejecucion = tiempoEjecucion,  // Tiempo de ejecución calculado
                    log_api_query = string.IsNullOrEmpty(query) ? "" : query,  // Consulta ejecutada, puede ser null si no se usa
                    created_at = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds  // Unix timestamp
                };

                // Guardar el log en la base de datos
                _context.LogApi.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateToken(object user)
        {
            var claims = new List<Claim>();

            if (user is Administrador administrador)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, administrador.administrador_email));
                claims.Add(new Claim("id", administrador.administrador_id.ToString()));
            }
            else if (user is Usuario usuario)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, usuario.usuario_username));
                claims.Add(new Claim("id", usuario.usuario_id.ToString()));
            }

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public static string HashPassword(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Computar el hash y convertirlo en un arreglo de bytes
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convertir el arreglo de bytes en un string hexadecimal
                StringBuilder builder = new StringBuilder();
                foreach (byte t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static bool SecureCompare(string a, string b)
        {
            if (a.Length != b.Length) return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i]; 
            }
            return result == 0;
        }

    }
}
