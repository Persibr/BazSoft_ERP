using Bazsoft_ERP.Models;
using Dapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. Agrega servicios necesarios
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // Puedes ajustar el tiempo de expiración según tu necesidad
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Configura Entity Framework
//builder.Services.AddDbContext<BazSoft_ERPContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configura DataProtection para almacenar claves de cifrado
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\keys")) // Asegúrate de que el AppPool tenga acceso
    .SetApplicationName("Montelirio");

// 4. Agrega soporte para MVC con vistas
builder.Services.AddControllersWithViews();


builder.Services.AddCors(options => options.AddDefaultPolicy(config =>
{
    config.AllowAnyMethod();
    config.AllowAnyHeader();
    config.AllowAnyOrigin();
}));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var statusCode = context.Response.StatusCode;

            // si es 401, no redirigimos, dejamos que llegue al cliente
            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return;
            }

            // en otros casos (500, etc.) sí redirigimos al login o error page
            var loginPath = context.Request.PathBase + "/UsuarioLogon/Login";
            context.Response.Redirect(loginPath);
        });
    });

    app.UseHsts();
}


app.UseCors();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ?? Usa Session antes de Authorization
app.UseSession();

app.UseAuthorization();

//// 🔹 Cargar menús globales (una sola vez)
//using (var connection = new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
//{
//    var menus = connection.Query<MenuItem>(
//        "spWeb_ObtenerMenuPorUsuario",
//        new { UserId = (int?)null },
//        commandType: CommandType.StoredProcedure
//    );

//    foreach (var menu in menus)
//    {
//        if (string.IsNullOrEmpty(menu.Menu_url_web)) continue;

//        var partes = menu.Menu_url_web.Split('/');
//        var publicController = partes[0]; // Dashboard
//        var publicAction = partes.Length > 1 ? partes[1] : "Index";

//        var internalController = menu.Menu_id; // PB600001
//        var pattern = $"{publicController}/{publicAction}";
//        app.MapControllerRoute(
//            name: menu.Menu_id,
//            pattern: pattern + "/{id?}",
//            defaults: new { controller = internalController, action = publicAction }
//        );
//    }
//}
using (var connection = new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    var menus = connection.Query<MenuItem>(
        "spWeb_ObtenerMenuPorUsuario",
        new { UserId = (int?)null },
        commandType: CommandType.StoredProcedure
    ).ToList();

    var registeredPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var menu in menus)
    {
        if (string.IsNullOrEmpty(menu.Menu_url_web)) continue;

        var partes = menu.Menu_url_web.Split('/');
        if (partes.Length < 2) continue;

        var publicController = partes[0].Trim(); // Alias público
        var publicAction = partes[1].Trim();
        var internalController = menu.Menu_id; // PB600001, etc.

        var pattern = $"{publicController}/{publicAction}";
        if (!registeredPatterns.Contains(pattern))
        {
            app.MapControllerRoute(
                name: menu.Menu_id + "_" + publicAction,
                pattern: pattern + "/{id?}",
                defaults: new { controller = internalController, action = publicAction }
            );
            registeredPatterns.Add(pattern);
        }
    }

    // 🔹 2️⃣ Registrar rutas de todas las acciones internas de los PB
    var pbControllers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(Controller)) && t.Name.StartsWith("PB"));

    foreach (var controller in pbControllers)
    {
        var controllerId = controller.Name.Replace("Controller", ""); // PB600001
        var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.ReturnType.IsSubclassOf(typeof(IActionResult)) || m.ReturnType == typeof(IActionResult));

        foreach (var action in actions)
        {
            // Busca un menú para este PB para usar como alias, si no existe usa PBxxxx como alias
            var menu = menus.FirstOrDefault(m => m.Menu_id == controllerId);
            string publicController = menu != null ? menu.Menu_url_web.Split('/')[0] : controllerId;
            string publicAction = action.Name;

            var pattern = $"{publicController}/{publicAction}";
            if (!registeredPatterns.Contains(pattern))
            {
                app.MapControllerRoute(
                    name: controller.Name + "_" + action.Name,
                    pattern: pattern + "/{id?}",
                    defaults: new { controller = controllerId, action = action.Name }
                );
                registeredPatterns.Add(pattern);
            }
        }
    }
}


// 6. Define las rutas por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=UsuarioLogon}/{action=Login}/{id?}");

app.Run();