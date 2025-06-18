namespace Inicio_Sesion_API.Models
{
    public class LoginAdminRequest
    {
        public string Email { get; set; } // Campo para el email del administrador
        public string Password { get; set; } // Campo para la contraseña
    }
}