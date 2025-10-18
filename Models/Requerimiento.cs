namespace Bazsoft_ERP.Models
{
    public class Requerimiento
    {
        public string ? IdRQCifrado {  get; set; }
        public string? IdRQ { get; set; }
        public string? Periodo { get; set; }
        public string ? Empresa { get; set; } 
        public DateTime FechaRegistro { get; set; }
        public string ? FechaCrea { get; set; }
        public string ? TipoOrden { get; set; }
        public string? Numero { get; set; }
        public string? TipoOrdenC { get; set; }
        public string ? UsuSoli { get; set; }
        public string ? UsuAprueba { get; set; }
        public string? TipoRq { get; set; }
        public string? TituloRq { get; set; }               
        public string? Estado{ get; set; }
        public string ? DetallesJson { get; set; }
        public List<DetalleRQ> Detalles { get; set; }

    }
    public class DetalleRQ
    {
        public int IdRqDet { get; set; }
        public string? IdRqDetC { get; set; }
        public int ProductoId { get; set; }        
        public string ? ProductoTexto { get; set; }  
        public int IdFactor { get; set; }          
        public string? Presentacion { get; set; }   
        public string? Unidad { get; set; }         
        public decimal Cantidad { get; set; }      
        public DateTime FechaEntrega { get; set; } 
        public int IdAnalit { get; set; }          
        public string? DestinoTexto { get; set; }   
        public string? Observacion { get; set; }    
    }

}

