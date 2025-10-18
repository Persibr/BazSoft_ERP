using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Bazsoft_ERP.Models;
using Microsoft.Data.SqlClient;

namespace Bazsoft_ERP.Controllers
{
    public class LayoutController : BaseController
    {
        public LayoutController(IConfiguration config) : base(config)
        {
        }

        [Route("Layout/CargarMenu")]
        public ActionResult CargarMenu()
        {
            int? userId = HttpContext.Session.GetInt32("IdUsuario");
            if (userId == null)            
            {
                // Enviar instrucción clara al cliente
                 return Unauthorized();
            }
            var parametros = new DynamicParameters();
            parametros.Add("@UserId", userId);

            using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                var menu = connection.Query<MenuItem>(
                                "spWeb_ObtenerMenuPorUsuario",
                                parametros,
                                commandType: CommandType.StoredProcedure
                           )?.ToList() ?? new List<MenuItem>();

                return PartialView("_MenuPartial", menu);
            }
        }
    }
}