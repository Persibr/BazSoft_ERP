using Bazsoft_ERP.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace Bazsoft_ERP.Controllers
{
    public class ProcesosController : BaseController
    {
     
        public ProcesosController(IConfiguration config) : base(config)
        {
        }
        #region "Listados"
        public IEnumerable<SelectListItem> ObtenerClientes()
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                return connection.Query("spWeb_Listar_Clientes",
                                    new { tipo = "4" },
                                        commandType: CommandType.StoredProcedure)
                                 .Select(v => new SelectListItem
                                 {
                                     Value = v.tab_codigo.ToString(),
                                     Text = v.tab_descri.ToString()
                                 });
            }
        }      

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

        #region "IndexRecojo"
        public IActionResult Recojo()
        {            
            string filtros = HttpContext.Session.GetString("FiltrosRecojo") ?? "";
            var partes = filtros.Split(',');

            DateTime desde = (!string.IsNullOrEmpty(partes[0]) ? DateTime.Parse(partes[0]) : DateTime.Now);
            DateTime hasta = (partes.Length > 1 && !string.IsNullOrEmpty(partes[1]) ? DateTime.Parse(partes[1]) : DateTime.Now);
            ViewBag.FechaDesde = desde.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = hasta.ToString("yyyy-MM-dd");           
            return View();
        }

        [HttpPost]
        public IActionResult ListarRecojosAjax(DateTime? FechaDesde, DateTime? FechaHasta)
        {
            var acceso = ObtenerAccesos("PB600602");
            var permisos = acceso.FirstOrDefault();
            bool puedeRegistrar = permisos?.user_acceso.Substring(0, 1) == "1";
            bool puedeEditar = permisos?.user_acceso.Substring(1, 1) == "1";
            bool puedeAnular = permisos?.user_acceso.Substring(2, 1) == "1";
            bool puedeImprimir = permisos?.user_acceso.Substring(3, 1) == "1";

            //// Revisar si hay filtros en sesión
            //FormaPago = string.IsNullOrWhiteSpace(FormaPago) ? null : FormaPago;
            //Vendedor = string.IsNullOrWhiteSpace(Vendedor) ? null : Vendedor;

            // 2. Revisar sesión solo si no vino nada del cliente
            //if (!FechaDesde.HasValue && !FechaHasta.HasValue && FormaPago == null && Vendedor == null)
            if (!FechaDesde.HasValue && !FechaHasta.HasValue)
            {
                string filtrosSesion = HttpContext.Session.GetString("FiltrosRecojo");
                if (!string.IsNullOrEmpty(filtrosSesion))
                {
                    var partes = filtrosSesion.Split(',');
                    FechaDesde = !string.IsNullOrEmpty(partes[0]) ? DateTime.Parse(partes[0]) : (DateTime?)null;
                    FechaHasta = !string.IsNullOrEmpty(partes[1]) ? DateTime.Parse(partes[1]) : (DateTime?)null;
                    //FormaPago = partes.Length > 2 && !string.IsNullOrWhiteSpace(partes[2]) ? partes[2] : null;
                    //Vendedor = partes.Length > 3 && !string.IsNullOrWhiteSpace(partes[3]) ? partes[3] : null;
                }
            }

            // Guardar filtros actuales en sesión
            //string filtros = $"{FechaDesde?.ToString("yyyy-MM-dd")},{FechaHasta?.ToString("yyyy-MM-dd")},{FormaPago ?? ""},{Vendedor ?? ""}";
            string filtros = $"{FechaDesde?.ToString("yyyy-MM-dd")},{FechaHasta?.ToString("yyyy-MM-dd")}";
            HttpContext.Session.SetString("FiltrosRecojo", filtros);

            using (var db = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var parametros = new DynamicParameters();
                parametros.Add("@FechaDesde", FechaDesde ?? DateTime.Today);
                parametros.Add("@FechaHasta", FechaHasta ?? DateTime.Today);
                parametros.Add("@Tipo","01");

                var lista = db.Query<Recojo>("spWeb_ListarProcesos", parametros, commandType: CommandType.StoredProcedure).ToList();

                // Mapear estado a HTML
                var listaDTO = lista.Select(x =>
                {
                    var idNotaC = CryptoHelper.CifrarId(x.IdNota.ToString());

                    return new
                    {
                        Id_NotaC = idNotaC,
                        FechaRecojo = x.FechaRecojo.ToString("dd-MM-yyyy"),
                        x.Hora,
                        x.Guia,
                        x.Clientes,
                        x.Estado,
                        x.ProcesoActual,
                        Acciones = $"<div class='d-flex justify-content-center gap-1'>" +
                                $"<button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-primary btn-sm btnInfo' title='Info'><i class='fas fa-info-circle'></i></button>" +
                                (x.Estado == "0" && puedeAnular
                                    ? $"<button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-danger btn-sm btnEliminar' title='Eliminar'><i class='fas fa-trash-alt'></i></button>"
                                    : "") +
                                (x.Estado == "0" && puedeEditar
                                    ? $"<button type='button' Id_NotaC='{idNotaC}' class='btn btn-outline-warning btn-sm btnEditar' title='Editar'><i class='fas fa-edit'></i></button>"
                                    : "") +
                                "</div>"
                    };
                }).ToList();
                //spWeb_ListarProcesos '20251001','20251001','01'

                //                spWeb_ProcesoTracking 12645,1,'Comun','2'
                //spWeb_ProcesoTracking 12645,2,'Comun','1'
                //spWeb_ProcesoTracking2 12645,'Comun'

                return Json(new { data = listaDTO });
            }
        }
        #endregion

        #region "Recojo"
        //public IActionResult RecojoAccion(string? idNota)
        //{
        //    var model = new Recojo();
        //    if (model.FechaRecojo == default)
        //        model.FechaRecojo = DateTime.Today;

        //    if (model.FechaGuia == default)
        //        model.FechaGuia = DateTime.Today;

        //    var userId = HttpContext.Session.GetInt32("IdUsuario");
        //    ViewBag.Clientes = ObtenerClientes();
        //    ViewBag.TiposCompra = ObtenerTipoCompra().ToList();
        //    ViewBag.Vehiculos = ObtenerVehiculos().ToList();
        //    ViewBag.Documentos = ObtenerTipoDocumentos().ToList();

        //    if (string.IsNullOrEmpty(idNota)) // NUEVO
        //    {
        //        ViewBag.IdNota = null;

        //        model.Documentos = "GUS";       // doc_codigo
        //        model.Serie = "T001";           // gui_serie (ajusta según corresponda)

        //        using (var cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //        {
        //            cn.Open();
        //            var cmd = new SqlCommand("spWeb_GuiasRecojo", cn);
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@tipo", "2");
        //            cmd.Parameters.AddWithValue("@id_almacen", 34);
        //            cmd.Parameters.AddWithValue("@not_anomes", DateTime.Now.ToString("yyyyMM"));

        //            var correlativo = cmd.ExecuteScalar()?.ToString();
        //            model.Correlativo = correlativo;  // gui_numero
        //        }

        //        // Productos vacíos (se cargan todos desde catálogo)
        //        ViewBag.ProductosJson = JsonConvert.SerializeObject(ObtenerProductos());
        //    }
        //    else // EDITAR
        //    {
        //        ViewBag.IdNota = idNota;
        //        var idreal = CryptoHelper.DescifrarId(idNota);
        //        using (var cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //        {
        //            cn.Open();

        //            // CABECERA
        //            var cmdCab = new SqlCommand("spWeb_PES_BazSoft_Manten_Procesos", cn);
        //            cmdCab.CommandType = CommandType.StoredProcedure;
        //            cmdCab.Parameters.AddWithValue("@Tipo", "01");
        //            cmdCab.Parameters.AddWithValue("@IdAlm", 34);
        //            cmdCab.Parameters.AddWithValue("@Id_Nota", idNota);

        //            using (var dr = cmdCab.ExecuteReader())
        //            {
        //                if (dr.Read())
        //                {
        //                    model.IdNota = Convert.ToInt32(dr["IdNota"]);
        //                    model.Correlativo = dr["NroNota"].ToString();
        //                    model.Documentos = dr["doc_codigo"].ToString();
        //                    model.Serie = dr["gui_serie"].ToString();
        //                    model.FechaRecojo = Convert.ToDateTime(dr["FecRegistro"]);
        //                    model.FechaGuia = Convert.ToDateTime(dr["FecGuia"]);
        //                    model.Vehiculos = dr["IdVeh"].ToString();
        //                }
        //            }

        //            // DETALLE
        //            var cmdDet = new SqlCommand("spWeb_PES_BazSoft_Manten_Procesos", cn);
        //            cmdDet.CommandType = CommandType.StoredProcedure;
        //            cmdDet.Parameters.AddWithValue("@Tipo", "02");
        //            cmdDet.Parameters.AddWithValue("@IdAlm", 34);
        //            cmdDet.Parameters.AddWithValue("@Id_Nota", idNota);

        //            var detalles = new List<DetalleRecojo>();
        //            using (var dr2 = cmdDet.ExecuteReader())
        //            {
        //                while (dr2.Read())
        //                {
        //                    var IdFactor = dr2["IdDDet"].ToString();
        //                    var ProductoId = dr2["IdProd"].ToString();
        //                    detalles.Add(new DetalleRecojo
        //                    {
        //                        IdDet = dr2["IdDet"].ToString(),
        //                        IdDDet = dr2["IdDDet"].ToString(),
        //                        IdFactorC = dr2["IdDDet"].ToString(),
        //                        ProductoIdC = dr2["IdProd"].ToString(),
        //                        ProductoTexto = dr2["Producto"].ToString(),
        //                        Cantidad = Convert.ToDecimal(dr2["Cantidad"])
        //                    });
        //                }
        //            }

        //            model.Detalles = detalles;
        //            // Preparo JSON de productos con info de detalle
        //            ViewBag.ProductosJson = JsonConvert.SerializeObject(detalles);
        //        }
        //    }

        //    return PartialView("_ModRecojo", model);
        //}
        [HttpGet]
        public IActionResult RecojoAccion(string? idNota)
        {
            var model = new Recojo();

            if (model.FechaRecojo == default)
                model.FechaRecojo = DateTime.Today;

            if (model.FechaGuia == default)
                model.FechaGuia = DateTime.Today;

            ViewBag.Clientes = ObtenerClientes();
            ViewBag.TiposCompra = ObtenerTipoCompra().ToList();
            ViewBag.Vehiculos = ObtenerVehiculos().ToList();
            ViewBag.Documentos = ObtenerTipoDocumentos().ToList();

            // Productos base
            var productosBase = ObtenerProductos(); // ya devuelven Id / IdFactor cifrados

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
        public IActionResult RecojoAccion(Recojo model, string DetallesJson, int? IdNota)
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
            decimal unidadxjaba;

            using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("spWeb_LogNotaCabe", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Tipo", '1');
                            cmd.Parameters.AddWithValue("@id_almacen", id_almacen);
                            cmd.Parameters.AddWithValue("@not_anomes", model.FechaRecojo.ToString("yyyyMM"));
                            cmd.Parameters.AddWithValue("@not_tipmov", "NTI");
                            cmd.Parameters.AddWithValue("@not_tipope", "PRO");

                            var paramNumero = new SqlParameter("@not_numero", SqlDbType.Char, 5) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(paramNumero);

                            cmd.ExecuteNonQuery();

                            not_numero = paramNumero.Value?.ToString();
                        }

                        using (SqlCommand cmd = new SqlCommand("spWeb_LogNotaCabe", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Tipo", '2');
                            cmd.Parameters.AddWithValue("@id_almacen", id_almacen);
                            cmd.Parameters.AddWithValue("@not_anomes", model.FechaRecojo.ToString("yyyyMM"));
                            cmd.Parameters.AddWithValue("@not_tipmov", "NTI");
                            cmd.Parameters.AddWithValue("@not_tipope", "PRO");

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
                        if (!IdNota.HasValue || IdNota == 0)
                        {
                            using (SqlCommand cmdProc = new SqlCommand("spWeb_ProcesoTracking", con, tran))
                            {
                                cmdProc.CommandType = CommandType.StoredProcedure;

                                cmdProc.Parameters.AddWithValue("@IdNota", identity);      // Id de la nota recién creada
                                cmdProc.Parameters.AddWithValue("@IdProceso", 1);          // Primer proceso, por ejemplo 'Recojo'
                                cmdProc.Parameters.AddWithValue("@Usuario", crea_usu);     // Usuario que inicia
                                cmdProc.Parameters.AddWithValue("@Tipo", '1');             // 1 = iniciar proceso

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

        //#region "Edit"

        //public IActionResult Edit(int idNota)
        //{
        //    string filtros = HttpContext.Session.GetString("FiltrosGuias") ?? "";
        //    var partes = filtros.Split(',');

        //    DateTime desde = (!string.IsNullOrEmpty(partes[0]) ? DateTime.Parse(partes[0]) : DateTime.Now);

        //    DateTime hasta = (partes.Length > 1 && !string.IsNullOrEmpty(partes[1]) ? DateTime.Parse(partes[1]) : DateTime.Now);
        //    ViewBag.FechaDesde = desde.ToString("yyyy-MM-dd");
        //    string fecdesde = desde.ToString("yyyyMMdd");
        //    ViewBag.FechaHasta = hasta.ToString("yyyy-MM-dd");
        //    string fechasta = hasta.ToString("yyyyMMdd");
        //    ViewBag.IdNota = idNota;

        //    Recojo model = new Recojo();
        //    List<DetalleCompra> detalles = new List<DetalleCompra>();

        //    using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //    {
        //        con.Open();

        //        // 1. Obtener CABECERA
        //        using (SqlCommand cmd = new SqlCommand("spWeb_PES_BazSoft_Manten_Guias", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@Tipo", "01");
        //            cmd.Parameters.AddWithValue("@IdAlm", 1046);
        //            cmd.Parameters.AddWithValue("@FechaDesde", fecdesde);
        //            cmd.Parameters.AddWithValue("@FechaHasta", fechasta);
        //            cmd.Parameters.AddWithValue("@Id_Nota", idNota);

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    model.FechaRecojo = DateTime.ParseExact(reader["FecRegistro"].ToString()!, "yyyyMMdd", null);
        //                    //model.TipoCompra = reader["MovTipo"].ToString()!.Substring(3); // "NTICO1" → "CO1"
        //                    //model.Proveedores = int.Parse(reader["IdDes"].ToString()!);
        //                    model.Documentos = reader["doc_codigo"].ToString();
        //                    model.Serie = reader["gui_serie"].ToString();
        //                    model.Correlativo = reader["gui_numero"].ToString();
        //                    model.FechaGuia = DateTime.ParseExact(reader["FecGuia"].ToString()!, "yyyyMMdd", null);
        //                    model.Vehiculos = reader["IdVeh"].ToString();
        //                    model.NroNota = reader["NroNota"].ToString();
        //                }
        //            }
        //        }

        //        // 2. Obtener DETALLE
        //        using (SqlCommand cmd = new SqlCommand("spWeb_PES_BazSoft_Manten_Guias", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@Tipo", "02");
        //            cmd.Parameters.AddWithValue("@IdAlm", 1046);
        //            cmd.Parameters.AddWithValue("@FechaDesde", desde);
        //            cmd.Parameters.AddWithValue("@FechaHasta", hasta);
        //            cmd.Parameters.AddWithValue("@Id_Nota", idNota);

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    detalles.Add(new DetalleCompra
        //                    {
        //                        IdDet =reader["IdDet"].ToString(),
        //                        IdDDet = reader["IdDDet"].ToString(),
        //                        ProductoId = reader["IdProd"].ToString(),
        //                        ProductoTexto = reader["CodProd"].ToString(),
        //                        TipoJabaId = reader["TipJaba"].ToString(),
        //                        CantJab = Convert.ToInt32(reader["Jabas"]),
        //                        UxJ = Convert.ToInt32(reader["UndxEnv"]),
        //                        TotUnid = Convert.ToInt32(reader["Unidades"]),
        //                        PesoNeto = Convert.ToDecimal(reader["PesNeto"]),
        //                        PesoBruto = Convert.ToDecimal(reader["PesBruto"]),
        //                        PN_Despa = Convert.ToDecimal(reader["PN_Despa"]),
        //                        PB_Despa = Convert.ToDecimal(reader["PB_Despa"]),                                
        //                        PN_Bonif = Convert.ToDecimal(reader["PN_Bonif"]),
        //                        PB_Bonif = Convert.ToDecimal(reader["PB_Bonif"]),
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    ViewBag.Detalles = JsonConvert.SerializeObject(detalles);
        //    ViewBag.Productos = ObtenerProductos();
        //    ViewBag.TipoJaba = ObtenerTiposJaba();
        //    ViewBag.TiposCompra = ObtenerTipoCompra().ToList();
        //    ViewBag.Proveedores = ObtenerProveedores().ToList();
        //    ViewBag.Vehiculos = ObtenerVehiculos().ToList();
        //    ViewBag.Documentos = ObtenerTipoDocumentos().ToList();

        //    return View("Edit", model);
        //}


        //[HttpPost]
        //public IActionResult Edit(Recojo model, string DetallesJson, int? IdNota)
        //{
        //    ViewBag.IdNota = IdNota;
        //    var detalles = JsonConvert.DeserializeObject<List<DetalleCompra>>(DetallesJson);

        //    //string tipo = "1";
        //    int? id_almacen = 1046;
        //    var userId = HttpContext.Session.GetInt32("IdUsuario");
        //    var crea_usu = HttpContext.Session.GetString("Nombre") ?? userId?.ToString() ?? "Desconocido";

        //    int? identity = null;
        //    string? siExiste = null;
        //    int? IdTty = null;
        //    string? idfactor = null;

        //    using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //    {
        //        con.Open();
        //        using (SqlTransaction tran = con.BeginTransaction())
        //        {
        //            try
        //            {
        //                using (SqlCommand cmd = new SqlCommand("spWeb_LogNotaCabe", con, tran))
        //                {
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@Tipo", '2');
        //                    cmd.Parameters.AddWithValue("@id_almacen", id_almacen);
        //                    cmd.Parameters.AddWithValue("@not_anomes", model.FechaRecojo.ToString("yyyyMM"));
        //                    cmd.Parameters.AddWithValue("@not_tipmov", "NTI");
        //                    //cmd.Parameters.AddWithValue("@not_tipope", model.TipoCompra);

        //                    cmd.Parameters.AddWithValue("@not_numero", model.NroNota);

        //                    cmd.Parameters.AddWithValue("@not_fecreg", model.FechaRecojo.ToString("yyyyMMdd"));
        //                    cmd.Parameters.AddWithValue("@doc_codigo", model.Documentos);
        //                    cmd.Parameters.AddWithValue("@gui_serie", model.Serie);
        //                    cmd.Parameters.AddWithValue("@gui_numero", model.Correlativo);
        //                    cmd.Parameters.AddWithValue("@gui_fecemi", model.FechaGuia.ToString("yyyyMMdd"));

        //                    //cmd.Parameters.AddWithValue("@id_analit", model.Proveedores);
        //                    cmd.Parameters.AddWithValue("@mon_codigo", "PEN");
        //                    cmd.Parameters.AddWithValue("@id_vehiculo", model.Vehiculos);
        //                    cmd.Parameters.AddWithValue("@alm_destino", 0);
        //                    cmd.Parameters.AddWithValue("@id_notrel", 0);
        //                    cmd.Parameters.AddWithValue("@not_estado", 0);
        //                    cmd.Parameters.AddWithValue("@st_anulado", 0);
        //                    cmd.Parameters.AddWithValue("@Crea_Usu", crea_usu);
        //                    cmd.Parameters.AddWithValue("@Crea_Maq", crea_usu);
        //                    cmd.Parameters.AddWithValue("@id_lote", 0);
        //                    cmd.Parameters.AddWithValue("@pes_codigo", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@bal_codigo", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@fec_PrgVta", model.FechaRecojo.ToString("yyyyMMdd"));
        //                    cmd.Parameters.AddWithValue("@ana_dircod", DBNull.Value);

        //                    var paramId = new SqlParameter("@Identity", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //                    var paramExiste = new SqlParameter("@SiExiste", SqlDbType.Char, 1) { Direction = ParameterDirection.Output };
        //                    cmd.Parameters.Add(paramId);
        //                    cmd.Parameters.Add(paramExiste);

        //                    cmd.Parameters.AddWithValue("@veh_chofer", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@veh_LicCon", DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@for_pago", "CRD");
        //                    cmd.Parameters.AddWithValue("@sel_analit", "Tod");
        //                    cmd.Parameters.AddWithValue("@sel_prod", "Tod");
        //                    cmd.Parameters.AddWithValue("@id_producto", 0);
        //                    cmd.Parameters.AddWithValue("@fac_coddoc", "");
        //                    cmd.Parameters.AddWithValue("@fac_serie", "");
        //                    cmd.Parameters.AddWithValue("@fac_numero", "");
        //                    cmd.Parameters.AddWithValue("@fac_fecemi", "");
        //                    cmd.Parameters.AddWithValue("@num_OCCompra", "");
        //                    cmd.Parameters.AddWithValue("@anl_motivo", "");
        //                    cmd.Parameters.AddWithValue("@Pto_Venta", "154O01");
        //                    cmd.Parameters.AddWithValue("@id_prove", 0);
        //                    cmd.Parameters.AddWithValue("@id_vehiculo2", 0);
        //                    cmd.Parameters.AddWithValue("@veh_chofer2", "");
        //                    cmd.Parameters.AddWithValue("@veh_LicCon2", "");

        //                    cmd.ExecuteNonQuery();

        //                    identity = (int?)paramId.Value;
        //                    siExiste = paramExiste.Value?.ToString();
        //                }

        //                // Insertar detalles
        //                foreach (var item in detalles!)
        //                {
        //                    if (item.ProductoId == "26")
        //                    { idfactor = "1"; }
        //                    else { idfactor = "2"; }
        //                    using (SqlCommand cmdDeta = new SqlCommand("spWeb_LogNotaDeta", con, tran))
        //                    {
        //                        cmdDeta.CommandType = CommandType.StoredProcedure;

        //                        cmdDeta.Parameters.AddWithValue("@Tipo", "2");
        //                        cmdDeta.Parameters.AddWithValue("@id_notdet", item.IdDet);
        //                        cmdDeta.Parameters.AddWithValue("@id_nota", identity);
        //                        cmdDeta.Parameters.AddWithValue("@id_producto", item.ProductoId);
        //                        cmdDeta.Parameters.AddWithValue("@id_factor", idfactor);
        //                        cmdDeta.Parameters.AddWithValue("@pro_unidad", item.UxJ);
        //                        cmdDeta.Parameters.AddWithValue("@pro_cantid", item.TotUnid);
        //                        cmdDeta.Parameters.AddWithValue("@pro_unidar", item.UxJ);
        //                        cmdDeta.Parameters.AddWithValue("@pro_cantir", item.CantJab);
        //                        cmdDeta.Parameters.AddWithValue("@pro_precio", 0);
        //                        cmdDeta.Parameters.AddWithValue("@tc_tipo", 0);
        //                        cmdDeta.Parameters.AddWithValue("@gra_nrogal", 0);
        //                        cmdDeta.Parameters.AddWithValue("@gra_nrocor", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_rq", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_rqdet", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_oc", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_ocdet", 0);
        //                        cmdDeta.Parameters.AddWithValue("@st_item", 0);

        //                        var paramIdTty = new SqlParameter("@IdTty", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //                        cmdDeta.Parameters.Add(paramIdTty);

        //                        cmdDeta.Parameters.AddWithValue("@id_lote", DBNull.Value);
        //                        cmdDeta.Parameters.AddWithValue("@id_odddet", 0);
        //                        cmdDeta.Parameters.AddWithValue("@pro_prevta", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_notaori", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_notdetori", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_notades", 0);
        //                        cmdDeta.Parameters.AddWithValue("@id_notdetdes", 0);
        //                        cmdDeta.Parameters.AddWithValue("@pro_pesbrt", item.PesoBruto);
        //                        cmdDeta.Parameters.AddWithValue("@pro_canjab", item.CantJab);
        //                        cmdDeta.Parameters.AddWithValue("@pn_despa", item.PN_Despa);
        //                        cmdDeta.Parameters.AddWithValue("@pb_despa", item.PB_Despa);
        //                        cmdDeta.Parameters.AddWithValue("@pn_bonif", item.PN_Bonif);
        //                        cmdDeta.Parameters.AddWithValue("@pb_bonif", item.PB_Bonif);
        //                        decimal PN_Facturar = item.PesoNeto - item.PN_Bonif;
        //                        cmdDeta.Parameters.AddWithValue("@pn_facturar", PN_Facturar);

        //                        cmdDeta.ExecuteNonQuery();

        //                        IdTty = (int?)paramIdTty.Value;
        //                        using (SqlCommand cmdDDet = new SqlCommand("spWeb_LogNotaDDet", con, tran))
        //                        {
        //                            cmdDDet.CommandType = CommandType.StoredProcedure;

        //                            cmdDDet.Parameters.AddWithValue("@Tipo", "2");
        //                            cmdDDet.Parameters.AddWithValue("@id_detdet", item.IdDDet); // nuevo registro
        //                            cmdDDet.Parameters.AddWithValue("@id_notdet", IdTty);
        //                            cmdDDet.Parameters.AddWithValue("@lot_nrogal", 0);
        //                            cmdDDet.Parameters.AddWithValue("@lot_nrocor", 0);
        //                            cmdDDet.Parameters.AddWithValue("@pes_tipo", "PNT");
        //                            cmdDDet.Parameters.AddWithValue("@nro_envase", item.CantJab);
        //                            cmdDDet.Parameters.AddWithValue("@nro_undenv", item.UxJ);
        //                            cmdDDet.Parameters.AddWithValue("@pro_peso", item.PesoNeto);
        //                            cmdDDet.Parameters.AddWithValue("@rel_detdet", 0);
        //                            cmdDDet.Parameters.AddWithValue("@Pro_obse", " ");
        //                            cmdDDet.Parameters.AddWithValue("@pro_Hora", DateTime.Now.ToString("HH:mm:ss"));
        //                            cmdDDet.Parameters.AddWithValue("@pro_peso0", item.PesoBruto);
        //                            cmdDDet.Parameters.AddWithValue("@pro_incub", item.TipoJabaId);
        //                            cmdDDet.Parameters.AddWithValue("@pro_ltori", " ");
        //                            cmdDDet.Parameters.AddWithValue("@DevTipo", "");
        //                            cmdDDet.Parameters.AddWithValue("@MorTipo", "");

        //                            var paramIdTty2 = new SqlParameter("@IdTty", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //                            cmdDDet.Parameters.Add(paramIdTty2);

        //                            cmdDDet.ExecuteNonQuery();
        //                        }
        //                    }
        //                }

        //                tran.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                tran.Rollback();
        //                TempData["Error"] = "Error al actualizar la compra: " + ex.Message;
        //                return RedirectToAction("Recojo");
        //            }
        //        }
        //    }

        //    return RedirectToAction("Index");
        //}
        //[HttpPost]
        //public IActionResult DeleteDetalle([FromBody] DetalleDeleteRequest request)
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        //        {
        //            connection.Execute("spWeb_PES_BazSoft_Manten_Guias_EliminarDD",
        //                new
        //                {
        //                    Tipo = request.Tipo,
        //                    IdDet = request.IdDet,
        //                    IdDDet = request.IdDDet
        //                },
        //                commandType: CommandType.StoredProcedure
        //            );
        //        }

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}
        //#endregion

    }
}
