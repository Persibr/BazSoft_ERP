using Microsoft.AspNetCore.DataProtection;

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

// 6. Define las rutas por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=UsuarioLogon}/{action=Login}/{id?}");

app.Run();