using Bazsoft_ERP.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;


namespace Bazsoft_ERP.Controllers
{
    public class UsuarioLogonController : Controller
    {
        protected readonly IConfiguration _config;

        public UsuarioLogonController(IConfiguration config)
        {
            _config = config; 
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(UsuarioLogon usuario)
        {
            using var db = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var parametros = new DynamicParameters();
            parametros.Add("@Usuario", usuario.UsuarioLogin);
            parametros.Add("@Clave", usuario.Clave);

            var usuarioLogueado = db.QueryFirstOrDefault<UsuarioLogon>(
                "spWeb_LoginUsuario",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            if (usuarioLogueado != null)
            {
                HttpContext.Session.SetInt32("IdUsuario", usuarioLogueado.IdUsuario);
                HttpContext.Session.SetString("Nombre", usuarioLogueado.Nombre!);
                HttpContext.Session.SetString("Rol", usuarioLogueado.Rol!);

                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

