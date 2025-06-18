namespace Inicio_Sesion_API.Models
{
    public class Usuario
    {
        public int usuario_id { get; set; }  // Clave primaria
        public string usuario_username { get; set; }
        public string usuario_password { get; set; }
        public string usuario_nombres { get; set; }
        public string? usuario_apellidos { get; set; }
        public string? usuario_rut { get; set; }
        public string? usuario_email { get; set; }
        public string? usuario_fono { get; set; }
        public string? usuario_url { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string usuario_sistema { get; set; }
        
        public string? usuario_cod_validacion { get; set; }
    }
}