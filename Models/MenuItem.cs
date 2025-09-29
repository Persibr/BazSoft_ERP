namespace Bazsoft_ERP.Models
{
    public class MenuItem
    {     
        public string? Menu_id { get; set; }
        public string? Menu_Descrip { get; set; }
        public int Menu_Tipo { get; set; }
        public string? Menu_Accion { get; set; }
        public string? Menu_Opcion { get; set; }
        public int Menu_Estado { get; set; }
        public string? Menu_Almacen { get; set; }
        public string? Menu_Barra { get; set; }
        public string? Menu_BarraAux { get; set; }
        public string? Menu_BarraFec { get; set; }
        public string? Menu_Libro { get; set; }
        public string? Menu_Empre { get; set; }
        public string? Menu_serie { get; set; }
        public string? Menu_Vende{ get; set; }
        public string? Menu_url_web { get; set; }

    }
    public class AccesoMenu
    {
        public string? Menu_Descrip { get; set; }
        public string? Menu_url_web { get; set; }
        public string? Menu_id { get; set; }
        public string? user_acceso { get; set; }
        public string? emp_codigo { get; set; }
        public string? alm_codigo { get; set; }
    }

}
