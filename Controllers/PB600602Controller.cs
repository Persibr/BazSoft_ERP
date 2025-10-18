using Bazsoft_ERP.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text;

namespace Bazsoft_ERP.Controllers
{
    public class PB600602Controller : BaseController
    {
     
        public PB600602Controller(IConfiguration config) : base(config)
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



        #endregion
        
        #region "IndexRecojo"
        public IActionResult Recojo()
        {
            return View();
        }

     
        [HttpPost]
        public IActionResult ListarRecojosPendientesAjax()
        {          

            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                
                var lista = db.Query<Recojo>(
                    "spWeb_ListarProcesosPendientes",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                var listaDTO = lista.Select(x =>
                {
                    var idNotaC = CryptoHelper.CifrarId(x.IdNota.ToString());
                    // Función para generar HTML con clase según el proceso
                    string Resaltar(string codigo, string hora)
                    {
                        if (hora == null) return "";
                        if (codigo == x.ProcesoActual) return $"<span class='activo'>{hora}</span>";
                        // procesos anteriores (codigos menores) se pintan gris claro
                        return string.Compare(codigo, x.ProcesoActual) < 0 ? $"<span class='completado'>{hora}</span>" : hora;
                    }

                    return new
                    {
                        x.Guia,
                        x.Clientes,
                        RecojoHora = Resaltar("001", x.RecojoHora),
                        RecepcionHora = Resaltar("002", x.RecepcionHora),
                        LavadoHora = Resaltar("003", x.LavadoHora),
                        SecadoHora = Resaltar("004", x.SecadoHora),
                        PlanchadoHora = Resaltar("005", x.PlanchadoHora),
                        DobladoHora = Resaltar("006", x.DobladoHora),
                        EmbolsadoHora = Resaltar("007", x.EmbolsadoHora),
                        DespachoHora = Resaltar("008", x.DespachoHora),
                        EntregadoHora = Resaltar("009", x.EntregadoHora),
                        Acciones =
                        $@"<div class='d-flex justify-content-center gap-1'>
                            <button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-primary btn-sm btnInfo' title='Info'>
                                <i class='fas fa-info-circle'></i>
                            </button>
                            <button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-success btn-sm btnConfirmar' title='Confirmar'>
                                <i class='fas fa-check'></i>
                            </button>
                        </div>"
                    };
                }).ToList();

                return Json(new { data = listaDTO });
            }
        }

        [HttpPost]
        public IActionResult ConfirmarProceso(string idNotaC)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("IdUsuario");
                var userNom = HttpContext.Session.GetString("Nombre") ?? userId?.ToString() ?? "Desconocido";
                int idNota = int.Parse(CryptoHelper.DescifrarId(idNotaC));

                using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    db.Execute("spWeb_ProcesoTracking2", new { IdNota = idNota , Usuario = userNom }, commandType: CommandType.StoredProcedure);
                }

                return Json(new { ok = true, mensaje = "Proceso confirmado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = "Error: " + ex.Message });
            }
        }

        
        #endregion

        #region "Recojo Modal"
     
        [HttpGet]
        public IActionResult RecojoAccion(string? idNota)
        {
            var model = new Recojo();
            var userId = HttpContext.Session.GetInt32("IdUsuario");
            if (model.FechaRecojo == default)
                model.FechaRecojo = DateTime.Today;

            if (model.FechaGuia == default)
                model.FechaGuia = DateTime.Today;

            ViewBag.Clientes = ObtenerClientes(userId);
            ViewBag.TiposCompra = ObtenerTipoCompra().ToList();
            ViewBag.Vehiculos = ObtenerVehiculos().ToList();
            ViewBag.Documentos = ObtenerTipoDocumentos().ToList();

            // Productos base
            var productosBase = ObtenerProductos(); 
            List<dynamic> productosConCantidades;

            if (idNota == null)
            {
                // NUEVO
                ViewBag.IdNota = null;
                model.Documentos = "GUS";
                model.Serie = "T001";

                using (var cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    cn.Open();
                    var cmd = new SqlCommand("spWeb_GuiasRecojo", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tipo", "2");
                    cmd.Parameters.AddWithValue("@id_almacen", 34);
                    cmd.Parameters.AddWithValue("@not_anomes", DateTime.Now.ToString("yyyyMM"));

                    var correlativo = cmd.ExecuteScalar()?.ToString();
                    model.Correlativo = correlativo;
                }

                // productos sin cantidades
                productosConCantidades = productosBase.Select(p => new
                {
                    p.Id,
                    p.IdFactor,
                    p.Descripcion,
                    p.Unidad,
                    Cantidad = 0,
                    IdDet = CryptoHelper.CifrarId("0"),
                    IdDDet = CryptoHelper.CifrarId("0")
                }).ToList<dynamic>();
            }
            else
            {
                // EDITAR → obtenemos cabecera y detalle desde SP
                int idNotaDescifrado = int.Parse(CryptoHelper.DescifrarId(idNota));
                ViewBag.IdNota = idNota;

                // cabecera (ejemplo con tu SP tipo 01)
                using (var cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    cn.Open();
                    var cabecera = cn.QueryFirstOrDefault("spWeb_PES_BazSoft_Manten_Procesos",
                        new { Tipo = "01", IdAlm = 34, Id_Nota = idNotaDescifrado },
                        commandType: CommandType.StoredProcedure);

                    if (cabecera != null)
                    {
                        model.IdNotaCifrado = idNota;
                        model.Clientes = cabecera.IdDes.ToString();
                        model.FechaRecojo = cabecera.FecRegistro;
                        model.Documentos = cabecera.doc_codigo;
                        model.Serie = cabecera.gui_serie;
                        model.Correlativo = cabecera.gui_numero;
                        model.FechaGuia = cabecera.FecGuia;
                        model.Vehiculos = cabecera.IdVeh.ToString();
                    }
                }

                // detalle productos
                IEnumerable<dynamic> productosDetalle;
                using (var cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    cn.Open();
                    productosDetalle = cn.Query("spWeb_PES_BazSoft_Manten_Procesos",
                        new { Tipo = "02", IdAlm = 34, Id_Nota = idNotaDescifrado },
                        commandType: CommandType.StoredProcedure);
                }

                // mezclamos con base
                productosConCantidades = productosBase.Select(p =>
                {
                    var idProd = int.Parse(CryptoHelper.DescifrarId(p.Id));
                    var idFac = int.Parse(CryptoHelper.DescifrarId(p.IdFactor));

                    var det = productosDetalle.FirstOrDefault(x => x.IdProd == idProd && x.IdFac == idFac);

                    return new
                    {
                        p.Id,
                        p.IdFactor,
                        p.Descripcion,
                        p.Unidad,
                        Cantidad = det?.Cantidad ?? 0,
                        IdDet = CryptoHelper.CifrarId((det?.IdDet ?? 0).ToString()),
                        IdDDet = CryptoHelper.CifrarId((det?.IdDDet ?? 0).ToString())
                    };
                }).ToList<dynamic>();
            }

            ViewBag.ProductosJson = JsonConvert.SerializeObject(productosConCantidades);

            return PartialView("_ModRecojo", model);
        }

        [HttpPost]
        public IActionResult RecojoAccion(Recojo model, string DetallesJson, string? IdNota)
        {
            var userId = HttpContext.Session.GetInt32("IdUsuario");
            var detalles = JsonConvert.DeserializeObject<List<DetalleRecojo>>(DetallesJson);
            ViewBag.IdNota = IdNota;
            int? id_almacen = 34;

            string crea_usu = HttpContext.Session.GetString("Nombre") ?? userId?.ToString() ?? "Desconocido";
            int? identity = null;
            string siExiste = null;
            string not_numero = null;
            int? IdTty = null;

            using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        if (IdNota == "" || IdNota == null)
                        {
                            using (SqlCommand cmd = new SqlCommand("spWeb_LogNotaCabe", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Tipo", '1');
                                cmd.Parameters.AddWithValue("@id_almacen", id_almacen);
                                cmd.Parameters.AddWithValue("@not_anomes", model.FechaRecojo.ToString("yyyyMM"));
                                cmd.Parameters.AddWithValue("@not_tipmov", "NTI");
                                cmd.Parameters.AddWithValue("@not_tipope", "LAV");

                                var paramNumero = new SqlParameter("@not_numero", SqlDbType.Char, 5) { Direction = ParameterDirection.Output };
                                cmd.Parameters.Add(paramNumero);

                                cmd.ExecuteNonQuery();

                                not_numero = paramNumero.Value?.ToString();
                            }
                        }
                        else
                        {
                            not_numero = CryptoHelper.DescifrarId(IdNota);
                        }

                        using (SqlCommand cmd = new SqlCommand("spWeb_LogNotaCabe", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Tipo", '2');
                            cmd.Parameters.AddWithValue("@id_almacen", id_almacen);
                            cmd.Parameters.AddWithValue("@not_anomes", model.FechaRecojo.ToString("yyyyMM"));
                            cmd.Parameters.AddWithValue("@not_tipmov", "NTI");
                            cmd.Parameters.AddWithValue("@not_tipope", "LAV");

                            cmd.Parameters.AddWithValue("@not_numero", not_numero);

                            cmd.Parameters.AddWithValue("@not_fecreg", model.FechaRecojo.ToString("yyyyMMdd"));
                            cmd.Parameters.AddWithValue("@doc_codigo", model.Documentos);
                            cmd.Parameters.AddWithValue("@gui_serie", model.Serie);
                            cmd.Parameters.AddWithValue("@gui_numero", model.Correlativo);
                            cmd.Parameters.AddWithValue("@gui_fecemi", model.FechaGuia.ToString("yyyyMMdd"));

                            cmd.Parameters.AddWithValue("@id_analit", model.Clientes);
                            cmd.Parameters.AddWithValue("@mon_codigo", "PEN");
                            cmd.Parameters.AddWithValue("@id_vehiculo", model.Vehiculos);
                            cmd.Parameters.AddWithValue("@alm_destino", 0);
                            cmd.Parameters.AddWithValue("@id_notrel", 0);
                            cmd.Parameters.AddWithValue("@not_estado", 0);
                            cmd.Parameters.AddWithValue("@st_anulado", 0);
                            cmd.Parameters.AddWithValue("@Crea_Usu", crea_usu);
                            cmd.Parameters.AddWithValue("@Crea_Maq", crea_usu);
                            cmd.Parameters.AddWithValue("@id_lote", model.Clientes);//revisar ok
                            cmd.Parameters.AddWithValue("@pes_codigo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@bal_codigo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@fec_PrgVta", model.FechaRecojo.ToString("yyyyMMdd"));
                            cmd.Parameters.AddWithValue("@ana_dircod", DBNull.Value);

                            var paramId = new SqlParameter("@Identity", SqlDbType.Int) { Direction = ParameterDirection.Output };
                            var paramExiste = new SqlParameter("@SiExiste", SqlDbType.Char, 1) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(paramId);
                            cmd.Parameters.Add(paramExiste);

                            cmd.Parameters.AddWithValue("@veh_chofer", DBNull.Value);
                            cmd.Parameters.AddWithValue("@veh_LicCon", DBNull.Value);
                            cmd.Parameters.AddWithValue("@for_pago", "CRD");
                            cmd.Parameters.AddWithValue("@sel_analit", "Tod");
                            cmd.Parameters.AddWithValue("@sel_prod", "Tod");
                            cmd.Parameters.AddWithValue("@id_producto", 0);
                            cmd.Parameters.AddWithValue("@fac_coddoc", "");
                            cmd.Parameters.AddWithValue("@fac_serie", "");
                            cmd.Parameters.AddWithValue("@fac_numero", "");
                            cmd.Parameters.AddWithValue("@fac_fecemi", "");
                            cmd.Parameters.AddWithValue("@num_OCCompra", "");
                            cmd.Parameters.AddWithValue("@anl_motivo", "");
                            cmd.Parameters.AddWithValue("@Pto_Venta", "");
                            cmd.Parameters.AddWithValue("@id_prove", 0);
                            cmd.Parameters.AddWithValue("@id_vehiculo2", 0);
                            cmd.Parameters.AddWithValue("@veh_chofer2", "");
                            cmd.Parameters.AddWithValue("@veh_LicCon2", "");

                            cmd.ExecuteNonQuery();

                            identity = (int?)paramId.Value;
                            siExiste = paramExiste.Value?.ToString();
                        }

                        // Insertar detalles
                        foreach (var item in detalles)
                        {
                            var idReal = CryptoHelper.DescifrarId(item.ProductoIdC);
                            var factorReal = CryptoHelper.DescifrarId(item.IdFactorC);
                            var iddetReal = CryptoHelper.DescifrarId(item.IdDet);
                            var idddetReal = CryptoHelper.DescifrarId(item.IdDDet);
                            using (SqlCommand cmdDeta = new SqlCommand("spWeb_LogNotaDeta", con, tran))
                            {
                                cmdDeta.CommandType = CommandType.StoredProcedure;

                                cmdDeta.Parameters.AddWithValue("@Tipo", "2");
                                cmdDeta.Parameters.AddWithValue("@id_notdet", iddetReal);
                                cmdDeta.Parameters.AddWithValue("@id_nota", identity);
                                cmdDeta.Parameters.AddWithValue("@id_producto", idReal);
                                cmdDeta.Parameters.AddWithValue("@id_factor", factorReal);
                                cmdDeta.Parameters.AddWithValue("@pro_unidad", 1);
                                cmdDeta.Parameters.AddWithValue("@pro_cantid", item.Cantidad);
                                cmdDeta.Parameters.AddWithValue("@pro_unidar", 1);
                                cmdDeta.Parameters.AddWithValue("@pro_cantir", item.Cantidad);
                                cmdDeta.Parameters.AddWithValue("@pro_precio", 0);
                                cmdDeta.Parameters.AddWithValue("@tc_tipo", 0);
                                cmdDeta.Parameters.AddWithValue("@gra_nrogal", 0);
                                cmdDeta.Parameters.AddWithValue("@gra_nrocor", 0);
                                cmdDeta.Parameters.AddWithValue("@id_rq", 0);
                                cmdDeta.Parameters.AddWithValue("@id_rqdet", 0);
                                cmdDeta.Parameters.AddWithValue("@id_oc", 0);
                                cmdDeta.Parameters.AddWithValue("@id_ocdet", 0);
                                cmdDeta.Parameters.AddWithValue("@st_item", 0);

                                var paramIdTty = new SqlParameter("@IdTty", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                cmdDeta.Parameters.Add(paramIdTty);

                                cmdDeta.Parameters.AddWithValue("@id_lote", 0);
                                cmdDeta.Parameters.AddWithValue("@id_odddet", 0);
                                cmdDeta.Parameters.AddWithValue("@pro_prevta", 0);
                                cmdDeta.Parameters.AddWithValue("@id_notaori", 0);
                                cmdDeta.Parameters.AddWithValue("@id_notdetori", 0);
                                cmdDeta.Parameters.AddWithValue("@id_notades", 0);
                                cmdDeta.Parameters.AddWithValue("@id_notdetdes", 0);
                                cmdDeta.Parameters.AddWithValue("@pro_pesbrt", 0);
                                cmdDeta.Parameters.AddWithValue("@pro_canjab", 0);
                                cmdDeta.Parameters.AddWithValue("@pn_despa", 0);
                                cmdDeta.Parameters.AddWithValue("@pb_despa", 0);
                                cmdDeta.Parameters.AddWithValue("@pn_bonif", 0);
                                cmdDeta.Parameters.AddWithValue("@pb_bonif", 0);
                                cmdDeta.Parameters.AddWithValue("@pn_facturar", 0);
                                cmdDeta.ExecuteNonQuery();

                                IdTty = (int?)paramIdTty.Value;
                                using (SqlCommand cmdDDet = new SqlCommand("spWeb_LogNotaDDet", con, tran))
                                {
                                    cmdDDet.CommandType = CommandType.StoredProcedure;

                                    cmdDDet.Parameters.AddWithValue("@Tipo", "2");
                                    cmdDDet.Parameters.AddWithValue("@id_detdet", idddetReal); // nuevo registro
                                    cmdDDet.Parameters.AddWithValue("@id_notdet", IdTty);
                                    cmdDDet.Parameters.AddWithValue("@lot_nrogal", 0);
                                    cmdDDet.Parameters.AddWithValue("@lot_nrocor", 0);
                                    cmdDDet.Parameters.AddWithValue("@pes_tipo", "PNT");
                                    cmdDDet.Parameters.AddWithValue("@nro_envase", item.Cantidad);
                                    cmdDDet.Parameters.AddWithValue("@nro_undenv", 1);
                                    cmdDDet.Parameters.AddWithValue("@pro_peso", 0);
                                    cmdDDet.Parameters.AddWithValue("@rel_detdet", 0);
                                    cmdDDet.Parameters.AddWithValue("@Pro_obse", " ");
                                    cmdDDet.Parameters.AddWithValue("@pro_Hora", DateTime.Now.ToString("HH:mm:ss"));
                                    cmdDDet.Parameters.AddWithValue("@pro_peso0", 0);
                                    cmdDDet.Parameters.AddWithValue("@pro_incub", 0);
                                    cmdDDet.Parameters.AddWithValue("@pro_ltori", " ");
                                    cmdDDet.Parameters.AddWithValue("@DevTipo", "");
                                    cmdDDet.Parameters.AddWithValue("@MorTipo", "");

                                    var paramIdTty2 = new SqlParameter("@IdTty", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                    cmdDDet.Parameters.Add(paramIdTty2);

                                    cmdDDet.ExecuteNonQuery();
                                }
                            }
                        }
                        if (IdNota == ""|| IdNota == null)
                        {
                            using (SqlCommand cmdProc = new SqlCommand("spWeb_ProcesoTracking2", con, tran))
                            {
                                cmdProc.CommandType = CommandType.StoredProcedure;

                                cmdProc.Parameters.AddWithValue("@IdNota", identity);      
                                cmdProc.Parameters.AddWithValue("@Usuario", crea_usu);     

                                cmdProc.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        TempData["Error"] = "Error al guardar el recojo: " + ex.Message;
                        return RedirectToAction("Recojo");
                    }
                }
            }

            return RedirectToAction("Recojo");
        }

        #endregion


    }
}
