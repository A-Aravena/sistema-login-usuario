namespace Inicio_Sesion_API.Models
{
    public class LogSistema
    {
        public int log_sistema_id { get; set; }  // Clave primaria
        public string? log_sistema_accion { get; set; }
        public DateTime? log_sistema_fecha { get; set; }
        public string? log_sistema_cambios_realizados { get; set; }
        public DateTime created_at { get; set; }
        public int administrador_id { get; set; }

        // Relación
        public Administrador Administrador { get; set; }
    }
}