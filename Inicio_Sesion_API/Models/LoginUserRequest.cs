namespace Inicio_Sesion_API.Models
{
    public class LoginUserRequest
    {
        public string Username { get; set; } // Campo para el usuario (usuario_username)
        public string Password { get; set; } // Campo para la contraseña
        public string Sistema { get; set; } // Campo para el sistema (bioemach_autocunsulta)
    }
}
