using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Bazsoft_ERP.Models;

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
}
