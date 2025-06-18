namespace Inicio_Sesion_API.Models
{
    public class Administrador
    {
        public int administrador_id { get; set; }  // Clave primaria
        public string? administrador_email { get; set; }
        public string? administrador_password { get; set; }
        public string administrador_nombres { get; set; }
        public string? administrador_apellidos { get; set; }
        public string? administrador_fono { get; set; }
        public string? administrador_cod_validacion { get; set; }
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public DateTime updated_at { get; set; } = DateTime.UtcNow;
    }
}