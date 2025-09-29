using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bazsoft_ERP.Models
{
    public class Dashboard
    {
    }

    public class ReporteCobranza
    {
        public string ?rec_fecEmision { get; set; }
        public decimal importe { get; set; }
        public decimal ventas { get; set; }
    }

    public class ReporteVendedor
    {
        public string ?Vendedor { get; set; }
        public decimal Ventas { get; set; }
        public decimal Cobras { get; set; }
    }

    public class ResumenCobranzaDto
    {
        public decimal PesoComprado { get; set; }
        public decimal PesoVendido { get; set; }
        public decimal ImporteVendido { get; set; }
        public decimal ImporteCobrado { get; set; }

        public decimal RatioPeso => PesoComprado == 0 ? 0 : PesoVendido / (PesoComprado + PesoSobraAyer);
        public decimal RatioCobro => ImporteVendido == 0 ? 0 : ImporteCobrado / ImporteVendido;

        public decimal PesoSobraAyer { get; set; }
        public decimal PesoSobraHoy { get; set; }
        public decimal PesoTotalComprado => PesoSobraAyer + PesoComprado;
        public decimal PesoTotalHoy => PesoVendido + PesoSobraHoy;

        public List<SelectListItem>? ListaTipoConsulta { get; set; }


    }

}
