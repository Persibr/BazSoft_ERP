using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using Bazsoft_ERP.Models;

namespace Bazsoft_ERP.Controllers
{
    public class PB600001Controller : BaseController
    {

        //private readonly IConfiguration _config;

        public PB600001Controller(IConfiguration config) : base(config)
        {
            //_config = config;
        }


     
        public IActionResult Index()
        {
            //var accesos = ObtenerAccesos("PB600001");
            //var emp_codigo = accesos[0].emp_codigo;
            //if (HttpContext.Session.GetInt32("IdUsuario") == null)
            //    return RedirectToAction("Login", "UsuarioLogon");


            //ViewBag.FechaDesde = DateTime.Today.ToString("yyyy-MM-dd");

            //ViewBag.TipoConsulta =   new List<SelectListItem>
            //    {
            //        new SelectListItem { Value = "0", Text = "TODO" },
            //        new SelectListItem { Value = "1", Text = "POLLO" }
            //    };

            return View();
        }


     
        public IActionResult GraficoCobranzas()
        {
            var accesos = ObtenerAccesos("PB600001");
            var emp_codigo = accesos[0].emp_codigo;
            List<ReporteCobranza> lista = new List<ReporteCobranza>();

            using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                lista = con.Query<ReporteCobranza>(
                    "spWeb_Grafico_CobranzaPorFechaPruebas",
                    new { empcodigo = emp_codigo },
                    commandType: CommandType.StoredProcedure
                ).ToList();
            }

            return Json(lista);
        }

        [HttpPost]
        public IActionResult GraficoVendedor(string fecha)
        {
            var accesos = ObtenerAccesos("PB600001");
            var emp_codigo = accesos[0].emp_codigo;
            List<ReporteVendedor> lista = new List<ReporteVendedor>();

            using (SqlConnection con = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                lista = con.Query<ReporteVendedor>(
                    "spWeb_Grafico_CobranzaPorVendedorPruebas",
                    new { Fecha = fecha , empcodigo = emp_codigo },
                    commandType: CommandType.StoredProcedure
                ).ToList();
            }

            return Json(lista);
        }

        [HttpPost]
        public IActionResult ObtenerResumenCobranzas(DateTime fecha, string filtro)
        {
            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var result = connection.QueryFirstOrDefault<ResumenCobranzaDto>(
                    "spweb_Grafico_Resumen_Cobranzas",
                    new { Fecha = fecha, Tipo = filtro },
                    commandType: CommandType.StoredProcedure
                );

                return Json(result);
            }
        }



    }
}