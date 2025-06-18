namespace Inicio_Sesion_API.Models
{
    public class LogApi
    {
        public int log_api_id { get; set; } // Clave primaria
        public string? log_api_metodo { get; set; }
        public string? log_api_url { get; set; }
        public string? log_api_tipo_respuesta { get; set; }
        public DateTime? log_api_fecha { get; set; }
        public string? log_api_parametros_entrada { get; set; }
        public string? log_api_parametros_salida { get; set; }
        public string? log_api_tiempo_ejecucion { get; set; }
        public string? log_api_query { get; set; }
        public int created_at { get; set; } // UNIX_TIMESTAMP de la fecha de creación
    }
}
