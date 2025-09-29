namespace Bazsoft_ERP.Models
{
    public class UsuarioLogon
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? UsuarioLogin { get; set; } // input
        public string? Clave { get; set; }         // input
        public string? Rol { get; set; }
        public string? Menu_url_web { get; set; }

    }
   
}
