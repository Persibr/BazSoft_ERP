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
    public class PB040104Controller : BaseController
    {
     
        public PB040104Controller(IConfiguration config) : base(config)
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

        #region "Inicio"

        public IActionResult Registro()
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
                    string accionesHtml = "";

                    // Si el usuario actual es el mismo que el aprobador del item, muestra botones
                    if (x.UsuSoli != null && x.UsuSoli.ToString().Equals(logUser, StringComparison.OrdinalIgnoreCase))
                    {
                        accionesHtml = $@"
                        <div class='d-flex justify-content-center gap-1'>
                            <button type='button' class='btn btn-success btn-sm btnEditar' title='Editar' data-id='{idRQC}'>
                                <i class='fas fa-edit'></i>
                            </button>                                          
                        </div>";
                    }                    

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
                        FechaCrea = x.FechaCrea.ToString("dd-MM-yyyy HH:mm:ss"),
                        Acciones = accionesHtml
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
                    if (x.apro_usudes != null && x.apro_usudes.ToString().Equals(logUser, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Convert.ToString(x.FechaAprobacion)))
                    
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


        #region "RQ Modal"

        [HttpGet]
        public IActionResult RQAccion(string? idNota)
        {
            var model = new Requerimiento();
            var userId = HttpContext.Session.GetInt32("IdUsuario");
            if (model.FechaRegistro == default)
                model.FechaRegistro = DateTime.Today;           

            string genTipoRQ = HttpContext.Session.GetString("GenTipoRQ");

            ViewBag.UsuAprueba = ObtenerUsuAprobaRQ().ToList();
            //ViewBag.TiposOrden = ObtenerTipoOrden().ToList();            
            var tiposOrden = ObtenerTipoOrden().ToList();
            if (tiposOrden.Any()) tiposOrden[0].Selected = true;
            model.TipoOrden = tiposOrden[0].Value;
            ViewBag.TiposOrdenC = tiposOrden;

            ViewBag.Empresas = ObtenerEmpresaxMenu("PB040104").ToList();
            model.UsuSoli = HttpContext.Session.GetString("Log");
            ViewBag.TiposRq = ObtenerTipoRQ().ToList();
            ViewBag.Productos = ObtenerProductosyServicios(1);
            ViewBag.Servicios = ObtenerProductosyServicios(2);           

            return PartialView("_ModRegistro", model);
        }

        [HttpPost]
        public IActionResult RQAccion(Requerimiento model)
        {
            var userId = HttpContext.Session.GetInt32("IdUsuario");

            string crea_nom = HttpContext.Session.GetString("Nombre") ?? userId?.ToString() ?? "Desconocido";
            string crea_usu = HttpContext.Session.GetString("Log") ?? userId?.ToString() ?? "Desconocido";
            string rq_numero = null;
            int id_rq = 0;
            if (!string.IsNullOrEmpty(model.DetallesJson))
            {
                model.Detalles = JsonConvert.DeserializeObject<List<DetalleRQ>>(model.DetallesJson);
            }else
            {
                model.Detalles = new List<DetalleRQ>();
            }

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("spWeb_smnt_LogRqCabe", conn, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Tipo", "1");
                            cmd.Parameters.AddWithValue("@Emp_Codigo", model.Empresa);
                            cmd.Parameters.AddWithValue("@rq_tipo", model.TipoOrdenC);
                            cmd.Parameters.AddWithValue("@rq_anomes", model.FechaRegistro.ToString("yyyyMM"));

                            var paramNumero = new SqlParameter("@rq_numero", SqlDbType.Char, 5) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(paramNumero);

                            cmd.ExecuteNonQuery();

                            rq_numero = paramNumero.Value?.ToString();
                        }
                        using (SqlCommand cmdCab = new SqlCommand("spWeb_smnt_LogRqCabe", conn, tran))
                        {
                            cmdCab.CommandType = CommandType.StoredProcedure;

                            cmdCab.Parameters.AddWithValue("@Tipo", "2");
                            cmdCab.Parameters.AddWithValue("@Emp_Codigo", model.Empresa);
                            cmdCab.Parameters.AddWithValue("@rq_tipo", model.TipoOrdenC);
                            cmdCab.Parameters.AddWithValue("@rq_anomes", model.FechaRegistro.ToString("yyyyMM"));
                            cmdCab.Parameters.AddWithValue("@rq_numero", rq_numero);
                            cmdCab.Parameters.AddWithValue("@rq_fecreg", model.FechaRegistro.ToString("yyyyMMdd"));
                            cmdCab.Parameters.AddWithValue("@rq_observa", model.TituloRq);
                            cmdCab.Parameters.AddWithValue("@st_anulado", "0");                            
                            cmdCab.Parameters.AddWithValue("@Crea_Usu", crea_usu);
                            cmdCab.Parameters.AddWithValue("@Crea_Maq", crea_nom);

                            var identityParam = new SqlParameter("@identity", SqlDbType.Int) { Direction = ParameterDirection.Output };
                            var idExisteParam = new SqlParameter("@SiExiste", SqlDbType.Char, 1) { Direction = ParameterDirection.Output };
                            
                            cmdCab.Parameters.Add(identityParam);
                            cmdCab.Parameters.Add(idExisteParam);
                            cmdCab.Parameters.AddWithValue("@Cod_TipRq", model.TipoRq);

                            cmdCab.ExecuteNonQuery();

                            id_rq = Convert.ToInt32(identityParam.Value);

                        }
                        foreach (var det in model.Detalles)
                        {                          
                            using (SqlCommand cmdDet = new SqlCommand("spWeb_smnt_LogRqDeta", conn, tran))
                            {
                                cmdDet.CommandType = CommandType.StoredProcedure;

                                cmdDet.Parameters.AddWithValue("@Tipo", "2");
                                cmdDet.Parameters.AddWithValue("@id_rqdet", 0);
                                cmdDet.Parameters.AddWithValue("@id_rq", id_rq);
                                cmdDet.Parameters.AddWithValue("@id_producto", det.ProductoId);
                                cmdDet.Parameters.AddWithValue("@id_factor", det.IdFactor);
                                cmdDet.Parameters.AddWithValue("@rq_cantid", det.Cantidad);
                                cmdDet.Parameters.AddWithValue("@rq_fecent", det.FechaEntrega.ToString("yyyyMMdd"));
                                cmdDet.Parameters.AddWithValue("@id_analit", det.IdAnalit);
                                cmdDet.Parameters.AddWithValue("@apro_est", 0);
                                cmdDet.Parameters.AddWithValue("@apro_usu", "");
                                cmdDet.Parameters.AddWithValue("@apro_fec", "");
                                cmdDet.Parameters.AddWithValue("@apro_obs", "");
                                cmdDet.Parameters.AddWithValue("@st_item", "0");
                                cmdDet.Parameters.AddWithValue("@pro_observ", det.Observacion);
                                cmdDet.Parameters.AddWithValue("@apro_usudes", model.UsuAprueba);

                                var idTtyParam = new SqlParameter("@idDet", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                cmdDet.Parameters.Add(idTtyParam);

                                cmdDet.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                        return Json(new { success = true, message = "Requerimiento registrado correctamente" });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Json(new { success = false, message = "Error al registrar: " + ex.Message });
                    }
                }
            }
        }


        [HttpGet]
        public IActionResult ObtenerCabeceraRQ(string idRQC)
        {
            int id_rq = int.Parse(CryptoHelper.DescifrarId(idRQC));

            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var cabecera = db.QueryFirstOrDefault<dynamic>(
                    "spWebLog_ObtenerCabeceraRQ",
                    new { id_rq },
                    commandType: CommandType.StoredProcedure
                );

                return Json(cabecera);
            }
        }

        [HttpPost]
        public IActionResult ListarDetalleRQ(int id_rq)
        {
            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var lista = db.Query<dynamic>(
                    "spWebLog_Listar_DetRQ",
                    new { id_rq },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(new { data = lista });
            }
        }

        #endregion

    }
}
