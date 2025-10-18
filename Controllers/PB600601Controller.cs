using Bazsoft_ERP.Models;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Bazsoft_ERP.Controllers
{
    public class PB600601Controller : BaseController
    {
     
        public PB600601Controller(IConfiguration config) : base(config)
        {
        }
        #region "Listados"
       

        public IEnumerable<SelectListItem> ObtenerVehiculos()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_ListarVehiculos",
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.Id_Analit.ToString(),
                                     Text = v.Ana_Codigo.ToString()
                                 });
            }
        }
        public IEnumerable<SelectListItem> ObtenerTipoCompra()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_ListarTipoCompras",
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.tab_codigo.ToString(),
                                     Text = v.tab_descri.ToString()
                                 });
            }
        }
        
        public IEnumerable<SelectListItem> ObtenerTipoDocumentos()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_Compras_ListarTipoDocumentos",
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.Doc_Codigo.ToString(),
                                     Text = v.Doc_Descri.ToString()
                                 });
            }
        }

        //public IEnumerable<object> ObtenerProductos()
        //{
        //    using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //    {
        //        var productos = connection.Query(
        //            "spWeb_ListarProductos2",
        //            commandType: CommandType.StoredProcedure
        //        ).ToList();

        //        // Mapear y devolver el formato que quieres
        //        return productos.Select(v => new
        //        {
        //            Id = CryptoHelper.CifrarId(v.id_producto.ToString()),   // 👈 aquí se cifra el ID
        //            IdFactor = CryptoHelper.CifrarId(v.id_factor.ToString()),
        //            Descripcion = v.pro_Descri,
        //            Unidad = v.uni_codigo,
        //        });
        //    }
        //}
        public IEnumerable<dynamic> ObtenerProductos()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var productos = connection.Query<dynamic>( // 👈 aquí
                    "spWeb_ListarProductos2",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return productos.Select(v => new
                {
                    Id = CryptoHelper.CifrarId(v.id_producto.ToString()),   // 👈 ya compila
                    IdFactor = CryptoHelper.CifrarId(v.id_factor.ToString()),
                    Descripcion = v.pro_Descri,
                    Unidad = v.uni_codigo,
                });
            }
        }



        #endregion

        #region "Inicio"

        public IActionResult Inicio()
        {
            string filtros = HttpContext.Session.GetString("FiltrosRecojo") ?? "";
            var partes = filtros.Split(',');
            var userId = HttpContext.Session.GetInt32("IdUsuario");

            DateTime desde = (!string.IsNullOrEmpty(partes[0]) ? DateTime.Parse(partes[0]) : DateTime.Now);
            DateTime hasta = (partes.Length > 1 && !string.IsNullOrEmpty(partes[1]) ? DateTime.Parse(partes[1]) : DateTime.Now);
            
            ViewBag.Clientes = ObtenerClientes(userId);
            ViewBag.FechaDesde = desde.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = hasta.ToString("yyyy-MM-dd");
            return View();
        }


        [HttpPost]
        public IActionResult ListarRecojosAjax(DateTime? FechaDesde, DateTime? FechaHasta)
        {
            FechaDesde ??= DateTime.Today;
            FechaHasta ??= DateTime.Today;

            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@FechaDesde", FechaDesde.Value.ToString("yyyyMMdd"));
                parametros.Add("@FechaHasta", FechaHasta.Value.ToString("yyyyMMdd"));

                var lista = db.Query<Recojo>(
                    "spWeb_ListarProcesos",
                    parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var listaDTO = lista.Select(x =>
                {
                    var idNotaC = CryptoHelper.CifrarId(x.IdNota.ToString());                    

                    return new
                    {
                        x.Guia,
                        x.Clientes,
                        x.RecojoHora,
                        x.RecepcionHora,
                        x.LavadoHora,
                        x.SecadoHora,
                        x.PlanchadoHora,
                        x.DobladoHora,
                        x.EmbolsadoHora,
                        x.DespachoHora,
                        x.EntregadoHora ,
                        Acciones =
                        $@"<div class='d-flex justify-content-center gap-1'>
                            <button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-primary btn-sm btnInfo' title='Info'>
                                <i class='fas fa-info-circle'></i>
                            </button>                            
                        </div>"
                    };
                }).ToList();

                return Json(new { data = listaDTO });
            }
        }

        #endregion


    }
}
