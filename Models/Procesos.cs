using System.ComponentModel.DataAnnotations;

namespace Bazsoft_ERP.Models
{
    public class Recojo
    {
        public int IdNota { get; set; }
        public string ? NroNota { get; set; }
        public DateTime FechaRecojo { get; set; }
        public string? TipoServicio { get; set; }
        //public int Proveedores { get; set; }
        public string? Vehiculos { get; set; }
        public string? Guia { get; set; }
        public string? Documentos { get; set; }
        public string? Serie { get; set; }
        public string? Correlativo { get; set; }
        public DateTime FechaGuia { get; set; }
        //public string? TipoVenta { get; set; }
        public string? Clientes { get; set; }
        public string? Hora { get; set; }
        public string? Estado { get; set; }
        public string ? IdNotaCifrado { get; set; }
        public List<DetalleRecojo> Detalles { get; set; }
        public string ? ProcesoActual { get; set; }
    }
    public class DetalleRecojo
    {
        public string? IdDet { get; set; }
        public string? IdDDet { get; set; }
        public string? IdFactor { get; set; }
        public string? IdFactorC { get; set; }
        public string? ProductoId { get; set; }
        public string? ProductoIdC { get; set; }
        public string? ProductoTexto { get; set; }
        public string? Medida { get; set; }
        public decimal Cantidad { get; set; }
    }
    public class Procesos
    {
        public int IdNota { get; set; }
        public string? Almacen { get; set; }
        public string? FecRegistro { get; set; }
        public string? MovDescri { get; set; }
        public string? ProvOrigen { get; set; }
        public string? NroGuia { get; set; }
        public string? FecGuia { get; set; }
        public string? Vehiculo { get; set; }
        public decimal Neto_Guia { get; set; }
        public decimal Neto_Despa { get; set; }
        public decimal Neto_Bonif { get; set; }
        public decimal Neto_Facturar { get; set; }
        public string? Estado { get; set; }
        public string? IdNotaCifrado { get; set; }

    }
    public class ProcesosDetalleGuias
    {
        public int IdNota { get; set; }
        public int IdDet { get; set; }
        public int IdDDet { get; set; }
        public int IdProd { get; set; }
        public int IdFac { get; set; }
        public string? CodProd { get; set; }
        public string? Producto { get; set; }
        public int Jabas { get; set; }
        public int UndxEnv { get; set; }
        public decimal Unidades { get; set; }
        public decimal PesNeto { get; set; }
        public decimal PesBruto { get; set; }
        public string? TipJaba { get; set; }
        public string? CodJaba { get; set; }
        public string? Jaba { get; set; }
        public int DesIdNota { get; set; }
        public int DesIdDet { get; set; }
        public int DesIdDDet { get; set; }
        public decimal PN_Despa { get; set; }
        public decimal PB_Despa { get; set; }
        public decimal PN_Bonif{ get; set; }
        public decimal PB_Bonif { get; set; }
        public decimal PN_Facturar { get; set; }
    }

    public class DetalleGuiasRRequest
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public int idnota { get; set; }
    }
   
    public class DetalleRDeleteRequest
    {
        public string? Tipo { get; set; }
        public int IdDet { get; set; }
        public int IdDDet { get; set; }
    }

}
