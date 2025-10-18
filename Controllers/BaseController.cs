using Bazsoft_ERP.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

public class BaseController : Controller
{
    protected readonly IConfiguration _config;

    public BaseController(IConfiguration config)
    {
        _config = config;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (HttpContext.Session.GetInt32("IdUsuario") == null)
        {
            var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                context.Result = new UnauthorizedResult(); // 401
            }
            else
            {
                context.Result = new RedirectToActionResult("Login", "UsuarioLogon", null);
            }
        }

        base.OnActionExecuting(context);
    }

    /// Método reutilizable para obtener accesos de menú desde cualquier controlador
    protected List<AccesoMenu> ObtenerAccesos(string menu_id)
    {
        // Recupera el usuario actual desde la sesión
        var id_usuario = HttpContext.Session.GetInt32("IdUsuario")?.ToString();

        if (string.IsNullOrEmpty(id_usuario))
            return new List<AccesoMenu>();

        using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            string procedure = "spWeb_user_accesosPruebas";

            var parametros = new
            {
                id_usuario,
                menu_id
            };

            return connection.Query<AccesoMenu>(
                procedure,
                param: parametros,
                commandType: System.Data.CommandType.StoredProcedure
            ).ToList();
        }
    }
    public IEnumerable<SelectListItem> ObtenerClientes(int? userId)
    {
        if (userId == null) return Enumerable.Empty<SelectListItem>();

        using (var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            return connection.Query("spWeb_Listar_Clientes_Zona",
                                    new { UserId = userId },
                                    commandType: CommandType.StoredProcedure)
                             .Select(v => new SelectListItem
                             {
                                 Value = v.tab_codigo.ToString(),
                                 Text = v.tab_descri.ToString()
                             });
        }
    }
}
