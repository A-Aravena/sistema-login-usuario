namespace Inicio_Sesion_API.Models
{
    public class Token
    {
        public int token_id { get; set; }
        public string token { get; set; }
        public string? token_login_type { get; set; }
        public DateTime? token_expires_at { get; set; }
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public int? usuario_id { get; set; }
        public int? administrador_id { get; set; }
    }
}