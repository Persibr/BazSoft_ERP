using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Bazsoft_ERP.Models;
namespace Bazsoft_ERP.Controllers
{
    public class MenuController : Controller
    {
        private readonly IConfiguration _config;

        public MenuController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            List<MenuItem> lista = new List<MenuItem>();

            using (SqlConnection cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                cn.Open();
                //string sql = "SELECT * FROM MA001020";
                //SqlCommand cmd = new SqlCommand(sql, cn);
                SqlCommand cmd = new SqlCommand("spWeb_CargaListadosMenu", cn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new MenuItem
                    {
                        Menu_id = dr["Menu_id"].ToString(),
                        Menu_Descrip = dr["Menu_Descrip"].ToString(),
                        Menu_Tipo = Convert.ToInt32(dr["Menu_Tipo"]),
                        Menu_Accion = dr["Menu_Accion"].ToString(),
                        Menu_Opcion = dr["Menu_Opcion"].ToString(),
                        Menu_Estado = Convert.ToInt32(dr["Menu_Estado"]),
                        Menu_Almacen = dr["Menu_Almacen"].ToString(),
                        Menu_Barra = dr["Menu_Barra"].ToString(),
                        Menu_BarraAux = dr["Menu_BarraAux"].ToString(),
                        Menu_BarraFec = dr["Menu_BarraFec"].ToString(),
                        Menu_Libro = dr["Menu_Libro"].ToString(),
                        Menu_url_web = dr["Menu_url_web"].ToString()
                    });
                }
            }

            return View(lista);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Crear(MenuItem menu)
        {
            using (SqlConnection cn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                cn.Open();
                //string sql = @"INSERT INTO MA001010 (Menu_id, Menu_Descrip, Menu_Tipo, Menu_Accion, Menu_Opcion, Menu_Estado, Menu_Almacen, Menu_Barra, Menu_BarraAux, Menu_BarraFec, Menu_Libro) 
                //               VALUES (@id, @descrip, @tipo, @accion, @opcion, @estado, @almacen, @barra, @barraAux, @barraFec, @libro)";
                SqlCommand cmd = new SqlCommand("spWeb_InsertarMenuItem", cn);
                //SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", menu.Menu_id);
                cmd.Parameters.AddWithValue("@descrip", menu.Menu_Descrip ?? "");
                cmd.Parameters.AddWithValue("@tipo", menu.Menu_Tipo);
                cmd.Parameters.AddWithValue("@accion", menu.Menu_Accion ?? "");
                cmd.Parameters.AddWithValue("@opcion", menu.Menu_Opcion ?? "");
                cmd.Parameters.AddWithValue("@estado", menu.Menu_Estado);
                cmd.Parameters.AddWithValue("@almacen", menu.Menu_Almacen ?? "");
                cmd.Parameters.AddWithValue("@barra", menu.Menu_Barra ?? "");
                cmd.Parameters.AddWithValue("@barraAux", menu.Menu_BarraAux ?? "");
                cmd.Parameters.AddWithValue("@barraFec", menu.Menu_BarraFec ?? "");
                cmd.Parameters.AddWithValue("@libro", menu.Menu_Libro ?? "");
                cmd.Parameters.AddWithValue("@url_web", menu.Menu_url_web ?? "");

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}