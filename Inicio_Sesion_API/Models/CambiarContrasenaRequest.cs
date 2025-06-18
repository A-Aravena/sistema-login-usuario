namespace Inicio_Sesion_API.Models
{
        public class CambiarContrasenaRequest
        {
            public string email { get; set; }
            public string codigo_validacion { get; set; }
            public string nueva_contraseña { get; set; }
        }
}