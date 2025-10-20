using Bazsoft_ERP.Models;
using Dapper;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Bazsoft_ERP.Controllers
{
    public class PB040106Controller : BaseController
    {
     
        public PB040106Controller(IConfiguration config) : base(config)
        {
        }
        #region "Listados"

        public IEnumerable<SelectListItem> ObtenerUsuAprobaRQ()
        {
            string logUser = HttpContext.Session.GetString("Log");

            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var data = connection.Query("spWeb_Logis_UsuAprobaRQ",
                                            new { logUser },
                                            commandType: CommandType.StoredProcedure)
                                     .ToList();

                string genTipoRQ = data.FirstOrDefault()?.GenTipoRQ?.Trim() ?? "";

                HttpContext.Session.SetString("GenTipoRQ", genTipoRQ);

                return data.Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Descri.ToString()
                }).ToList();
            }
        }

        public IEnumerable<SelectListItem> ObtenerEmpresaxMenu(string menu_id)
        {
            var user_id = HttpContext.Session.GetInt32("IdUsuario");
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_EmpxMenu", new { @user_id = user_id, @menu_id=menu_id },
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.emp_codigo.ToString(),
                                     Text = (v.emp_codigo.ToString() + " - " + v.Emp_Descri.ToString())
                                 });
            }
        }
        public IEnumerable<SelectListItem> ObtenerTipoRQ()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_Listar_Tipo_RQ",
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.tab_codigo.ToString(),
                                     Text = v.tab_descri.ToString()
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
                var productos = connection.Query<dynamic>(
                    "spWeb_ListarProductos2",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return productos.Select(v => new
                {
                    Id = CryptoHelper.CifrarId(v.id_producto.ToString()),   
                    IdFactor = CryptoHelper.CifrarId(v.id_factor.ToString()),
                    Descripcion = v.pro_Descri,
                    Unidad = v.uni_codigo,
                });
            }
        }

        public IEnumerable<dynamic> ObtenerProductosyServicios(int tipo)
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var data = connection.Query("spWeb_Listar_ProductosyServicios",
                                            new { Tipo = tipo },
                                            commandType: CommandType.StoredProcedure);

                return data.Select(v => new 
                {
                    Value = v.id_producto.ToString(),
                    Text = $"{v.pro_Abrevi} ({v.pro_codigo})",
                    UniCodigo = v.uni_codigo?.ToString(),
                    UniId = v.Id_Unidad?.ToString(),
                    DiaRep = v.pro_diarep.ToString()
                });
            }
        }

        public IEnumerable<SelectListItem> ObtenerTipoOrden()
        {
            string GenTipoRQ = HttpContext.Session.GetString("GenTipoRQ");

            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_Listar_Tipo_OC",
                                        new { @GenTipoRQ = GenTipoRQ },
                                        commandType: CommandType.StoredProcedure)
                                        .Select(v => new SelectListItem
                                        {
                                            Value = v.tab_codigo.ToString(),
                                            Text = v.tab_descri.ToString()
                                        });
            }
        }
        public IEnumerable<SelectListItem> ObtenerPresentacion(int id_producto)
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_Obtener_PresentacionxProducto",
                                        new { @id_producto = id_producto },
                                        commandType: CommandType.StoredProcedure)
                                        .Select(v => new SelectListItem
                                        {
                                            Value = v.id_factor.ToString(),
                                            Text = v.presentacion.ToString()
                                        });
            }
        }

       
        [HttpGet]
        public IActionResult GetDestinosPorEmpresa(string emp_codigo)
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var destinos = connection.Query(
                    "spWeb_Listar_DestinosxEmp",
                    new { emp_codigo },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(destinos);
            }
        }

        [HttpGet]
        public IActionResult GetPresentacionesPorProducto(int id_producto)
        {
            var presentaciones = ObtenerPresentacion(id_producto);
            return Json(presentaciones);
        }


        #endregion

        #region "Aprobar"

        public IActionResult Aprobar()
        {
            string logUser = HttpContext.Session.GetString("Log");
            ViewBag.UsuAprueba = ObtenerUsuAprobaRQ();

            string filtros = HttpContext.Session.GetString("FiltrosRQ") ?? "";
            var partes = filtros.Split(',');
            var userId = HttpContext.Session.GetInt32("IdUsuario");

            DateTime desde = (!string.IsNullOrEmpty(partes[0]) ? DateTime.Parse(partes[0]) : DateTime.Now);
            DateTime hasta = (partes.Length > 1 && !string.IsNullOrEmpty(partes[1]) ? DateTime.Parse(partes[1]) : DateTime.Now);
            ViewBag.LEmpresas = ObtenerEmpresaxMenu("PB040104").ToList();
            //ViewBag.Clientes = ObtenerClientes(userId);
            ViewBag.Periodo = DateTime.Now.ToString("yyyy-MM");
            ViewBag.TiposOrden = ObtenerTipoOrden();            
            return View();
        }


        [HttpPost]
        public IActionResult ListarRQAjax(string emp_codigo, string rq_tipo, string rq_anomes)
        {
            string logUser = HttpContext.Session.GetString("Log"); // usuario actual logueado
            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@emp_codigo", emp_codigo);
                parametros.Add("@rq_tipo", rq_tipo);
                parametros.Add("@rq_anomes", rq_anomes);

                var lista = db.Query<Requerimiento>(
                    "spWebLog_Listar_Rq",
                    parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var listaDTO = lista.Select(x =>
                {
                    var idRQC = CryptoHelper.CifrarId(x.IdRQ.ToString());

                    return new
                    {
                        idRQC,
                        x.TipoOrden,
                        x.Periodo,
                        x.Numero,
                        x.FechaRegistro,
                        x.TituloRq,
                        x.Estado,
                        x.UsuSoli,
                        x.TipoRq,
                        x.FechaCrea
                    };
                }).ToList();

                return Json(new { data = listaDTO });
            }
        }
        [HttpPost]
        public IActionResult ListarDetalleRQ(string id_rqC)
        {
            int id_rq = int.Parse(CryptoHelper.DescifrarId(id_rqC));
            string logUser = HttpContext.Session.GetString("Log"); // usuario actual logueado

            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@id_rq", id_rq);

                var lista = db.Query<dynamic>(
                    "spWebLog_Listar_DetRQ",
                    parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var listaDTO = lista.Select(x =>
                {
                    // Botones por defecto (vacío)
                    string accionesHtml = "";

                    // Si el usuario actual es el mismo que el aprobador del item, muestra botones
                    if (x.apro_usu != null && x.apro_usu.ToString().Equals(logUser, StringComparison.OrdinalIgnoreCase)&& x.FechaAprobacion == null)
                    {
                        accionesHtml = $@"
                        <div class='d-flex justify-content-center gap-1'>
                            <button type='button' class='btn btn-success btn-sm btnAprobar' title='Aprobar' data-id='{x.id_rqdet}'>
                                <i class='fas fa-check'></i>
                            </button>
                            <button type='button' class='btn btn-danger btn-sm btnRechazar' title='Rechazar' data-id='{x.id_rqdet}'>
                                <i class='fas fa-times'></i>
                            </button>                        
                        </div>";
                    }

                    return new
                    {
                        x.id_rqdet,
                        x.id_producto,  
                        x.pro_Observ,
                        x.pro_Abrevi,
                        x.rq_cantid,
                        x.FechaEntrega,
                        x.Ana_Descri,
                        x.EstadoAprobado,
                        x.apro_usudes,
                        x.FechaAprobacion,
                        Acciones = accionesHtml
                    };
                }).ToList();

                return Json(new { data = listaDTO });
            }
        }

        #endregion


    }
}
