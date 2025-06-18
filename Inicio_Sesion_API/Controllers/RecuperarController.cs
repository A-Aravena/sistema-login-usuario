using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inicio_Sesion_API.Data;
using Inicio_Sesion_API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;


namespace Inicio_Sesion_API.Controllers
{
    [ApiController]
    [Route("recuperar")]
    public class RecuperarController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();

        private readonly HttpClient _httpClient = new HttpClient();


        public RecuperarController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }



        [HttpPost("validar-email")]
        public async Task<IActionResult> ValidarEmail([FromBody] EmailRequest request)
        {
            var tiempoInicio = DateTime.UtcNow;
            string query = "SELECT * FROM Administrador WHERE administrador_email = @Email";

            if (request == null || string.IsNullOrEmpty(request.email))
            {
                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/validar-email", "400", "", "El email no puede estar vacío.", executionTime.TotalMilliseconds.ToString(), query);
                return BadRequest(new { mensaje = "El email no puede estar vacío." });
            }

            var administradores = await _context.Administrador
                .Where(a => a.administrador_email == request.email)
                .ToListAsync();

            if (administradores.Any())
            {
                string codigoValidacion = GenerarCodigoValidacion();

                foreach (var administrador in administradores)
                {
                    administrador.administrador_cod_validacion = codigoValidacion;
                }

                await _context.SaveChangesAsync();

                // Enviar correo
                bool emailEnviado = await EnviarCorreo(request.email, codigoValidacion);
                if (!emailEnviado)
                {
                    return StatusCode(500, new { mensaje = "Error al enviar el correo de validación." });
                }

                var response = new { mensaje = "Correo existente y código enviado" };

                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/validar-email", "200", "", JsonConvert.SerializeObject(response), executionTime.TotalMilliseconds.ToString(), query);
                return Ok(response);
            }

            var executionTimeNotFound = DateTime.UtcNow - tiempoInicio;
            await CrearLogApi("POST", "/recuperar/validar-email", "404", "", "Correo no registrado", executionTimeNotFound.TotalMilliseconds.ToString(), query);
            return NotFound(new { mensaje = "Correo no registrado" });
        }
private async Task<bool> EnviarCorreo(string destinatario, string codigo)
{
    string url = "http://rest.bioemach.cl/index.php/emailapi/enviarcorreo";

    string remitente = "notificaciones@bioemach.bioemach.cl";
    string asunto = "Restablecer clave";

    // Construcción del mensaje en HTML
    string header = $"<p style='text-align: center; font-size: 12px; color: rgb(153, 153, 153) !important;'>Para asegurar la entrega de nuestros correos electrónicos en su casilla de email, por favor agregue {remitente} a su libreta de direcciones.</p>";

    string warning = "<p style='text-align: center; font-style: italic; border: 1px solid #ccc; border-radius: 5px; width: 75%; margin: 0 auto; padding: 4px; background: floralwhite;'>Por favor, <strong>no responda este correo</strong>. Nadie revisa esta cuenta, por lo que <strong>no obtendrá respuesta</strong>.</p>";

    string footer = "<p style='text-align: center; font-size: 10px; color: rgb(153, 153, 153);'>Este correo fue generado automáticamente, por favor no responda.</p>";

    string mensajePrincipal = $"<p>El código de validación para recuperar su clave de acceso es: <br><br><span style='font-size: 24px; font-weight: bold;'>{codigo}</span><br><br></p>";

    string cuerpoCorreo = $"{header}{mensajePrincipal}{warning}{footer}";

    // Codificar en Base64 
    string cuerpoBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(cuerpoCorreo));

    // Crear los parámetros de la solicitud
    var fields = new Dictionary<string, string>
    {
        { "origen", "recuperacion_pass_bioemach" },
        { "destinatario", destinatario },
        { "remitente", remitente },
        { "asunto", asunto },
        { "nombre_remitente", "BioEmach Cloud" },
        { "cuerpo", cuerpoBase64 },
        { "userid", "77500240" },
        { "enviador", "appbioemach" }
    };

    var content = new FormUrlEncodedContent(fields);

    try
    {
        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error al enviar el correo. Código de estado: {response.StatusCode}");
        }

        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al enviar el correo: {ex.Message}");
        return false;
    }
}

        [HttpPost("validar-codigo")]
        public async Task<IActionResult> ValidarCodigo([FromBody] CodigoValidacionRequest request)
        {
            var tiempoInicio = DateTime.UtcNow;
            string query = "SELECT * FROM Administrador WHERE administrador_email = @Email";

            if (request == null || string.IsNullOrEmpty(request.email) || string.IsNullOrEmpty(request.codigo_validacion))
            {
                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/validar-codigo", "400", "", "El email y el código de validación no pueden estar vacíos.", executionTime.TotalMilliseconds.ToString(), query);
                return BadRequest(new { mensaje = "El email y el código de validación no pueden estar vacíos." });
            }

            var administradores = await _context.Administrador
                .Where(a => a.administrador_email == request.email)
                .ToListAsync();

            bool codigoValido = administradores.Any(a => a.administrador_cod_validacion == request.codigo_validacion);

            if (codigoValido)
            {
                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/validar-codigo", "200", "", "Código válido", executionTime.TotalMilliseconds.ToString(), query);
                return Ok(new { mensaje = "Código válido." });
            }

            foreach (var admin in administradores)
            {
                admin.administrador_cod_validacion = null;
            }
            await _context.SaveChangesAsync();

            var executionTimeNotFound = DateTime.UtcNow - tiempoInicio;
            await CrearLogApi("POST", "/recuperar/validar-codigo", "404", "", "Código inválido", executionTimeNotFound.TotalMilliseconds.ToString(), query);
            return NotFound(new { mensaje = "Código inválido." });
        }

        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request)
        {
            var tiempoInicio = DateTime.UtcNow;
            string query = "SELECT * FROM Administrador WHERE administrador_email = @Email";

            if (request == null || string.IsNullOrEmpty(request.email) || string.IsNullOrEmpty(request.codigo_validacion) || string.IsNullOrEmpty(request.nueva_contraseña))
            {
                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/cambiar-contrasena", "400", "", "Todos los campos son obligatorios.", executionTime.TotalMilliseconds.ToString(), query);
                return BadRequest(new { mensaje = "Todos los campos son obligatorios." });
            }

            var administradores = await _context.Administrador
                .Where(a => a.administrador_email == request.email)
                .ToListAsync();

            var adminValido = administradores.FirstOrDefault(a => a.administrador_cod_validacion == request.codigo_validacion);

            if (adminValido == null)
            {
                var executionTime = DateTime.UtcNow - tiempoInicio;
                await CrearLogApi("POST", "/recuperar/cambiar-contrasena", "404", "", "Código de validación incorrecto", executionTime.TotalMilliseconds.ToString(), query);
                return NotFound(new { mensaje = "Código de validación incorrecto." });
            }

            string contraseñaHash = GenerarHashSHA256(request.nueva_contraseña);

            foreach (var admin in administradores)
            {
                admin.administrador_password = contraseñaHash;
                admin.administrador_cod_validacion = null;
            }

            await _context.SaveChangesAsync();

            var executionTimeSuccess = DateTime.UtcNow - tiempoInicio;
            await CrearLogApi("POST", "/recuperar/cambiar-contrasena", "200", "", "Contraseña cambiada exitosamente", executionTimeSuccess.TotalMilliseconds.ToString(), query);
            return Ok(new { mensaje = "Contraseña cambiada exitosamente." });
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
        private string GenerarHashSHA256(string contraseña)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(contraseña);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        private string GenerarCodigoValidacion(int longitud = 5)
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(caracteres, longitud)
                                        .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> EnviarCorreoAsync(string destinatario, string codigo)
        {
            try
            {
                var remitente = "notificaciones@bioemach.bioemach.cl";
                var clave = "tu_contraseña"; // ⚠️ Usa un secreto seguro, no hardcodees

                var smtpClient = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(remitente, clave),
                    EnableSsl = true
                };

                var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente, "BioEmach Cloud"),
                    Subject = "Código de Validación",
                    Body = $"<p>Su código de validación es: <b>{codigo}</b></p>",
                    IsBodyHtml = true
                };

                mensaje.To.Add(destinatario);

                await smtpClient.SendMailAsync(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                return false;
            }
        }


    }


}