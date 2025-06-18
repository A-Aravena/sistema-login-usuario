namespace Inicio_Sesion_API.Models
{
    public class Configuracion
    {
        public int configuracion_id { get; set; } // Clave primaria
        public bool configuracion_log_enable { get; set; }
        public int configuracion_nivel_log_api { get; set; }
        public int? configuracion_tiempo_retencion { get; set; }
        public DateTime created_at { get; set; }
        public DateTime update_at { get; set; }
    }
}