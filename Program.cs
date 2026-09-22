using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string: prioriza variable de entorno (Render/Supabase)
var connectionString =
    Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configúrala en Render como env var CONNECTIONSTRINGS__DEFAULTCONNECTION.");

// Si viene como DATABASE_URL de estilo postgres://... convertir a Npgsql
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    connectionString = ConvertPostgresUrl(connectionString);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    // Detrás del proxy de Render
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS solo si Render termina TLS (sí lo hace)
    app.UseHsts();
}

// En Render el contenedor escucha HTTP; el proxy maneja HTTPS.
// UseHttpsRedirection puede romper health checks si no hay HTTPS local.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

static string ConvertPostgresUrl(string url)
{
    // postgres://user:pass@host:port/db
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var user = Uri.UnescapeDataString(userInfo[0]);
    var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var db = uri.AbsolutePath.Trim('/');
    return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
}

static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await db.Database.MigrateAsync();

        // Tabla Sales (gráfica) si aún no existe — compatible con Supabase
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS "Sales" (
    "Id" SERIAL PRIMARY KEY,
    "ProductId" integer NOT NULL,
    "ProductName" character varying(150) NOT NULL,
    "Category" character varying(100) NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "SoldAt" timestamp with time zone NOT NULL,
    "Notes" character varying(300) NULL
);
CREATE INDEX IF NOT EXISTS "IX_Sales_SoldAt" ON "Sales" ("SoldAt");
""");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR migrando base de datos: " + ex.Message);
            throw;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrador Tropivalle",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException("No se pudo crear el administrador: " + errors);
                }
            }
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Si hay productos viejos (Individual/Pack) o la tabla está vacía, cargar catálogo real
        var hasLegacy = await db.Products.AnyAsync(p =>
            p.Category == "Individual" || p.Category == "Pack x12" ||
            (p.Name != null && p.Name.Contains("Jugo de")));
        var isEmpty = !await db.Products.AnyAsync();

        if (isEmpty || hasLegacy)
        {
            if (hasLegacy)
            {
                db.Products.RemoveRange(db.Products);
                await db.SaveChangesAsync();
                Console.WriteLine("Productos antiguos eliminados.");
            }

            var products = new List<Product>
            {
                new() { Name = "TropiValle 1 Litro Retornable", Description = "Caja 12 unidades. Sabores Durazno y Manzana.", Price = 90.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-1l-retornable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "TropiValle 2 Litros Descartable", Description = "Paquete 6 unidades. Sabores Durazno y Manzana. Tumbo", Price = 72.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-2l-descartable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "TropiValle 300ml Retornable", Description = "Caja 12 unidades. Sabores Durazno y Manzana.", Price = 36.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-300ml-retornable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "TropiValle 330ml Descartable", Description = "Caja 12 unidades. Sabores Durazno y Manzana.", Price = 36.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-330ml-descartable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "TropiValle 620ml Retornable", Description = "Caja 12 unidades. Sabores Durazno y Manzana.", Price = 60.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-620ml-retornable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "TropiValle 630ml Descartable", Description = "Paquete 12 unidades. Sabores Durazno y Manzana.", Price = 60.00m, Stock = 100, Category = "Nectar TropiValle", ImageUrl = "/images/nectar-630ml-descartable.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "Agua de Mesa 2 Litros", Description = "Paquete de 6 unidades. Agua tratada y purificada con la más alta tecnología.", Price = 30.00m, Stock = 100, Category = "Agua de Mesa", ImageUrl = "/images/agua-2l.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "Agua de Mesa 330ml", Description = "Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.", Price = 24.00m, Stock = 100, Category = "Agua de Mesa", ImageUrl = "/images/agua-330ml.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "Agua de Mesa 630ml", Description = "Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.", Price = 36.00m, Stock = 100, Category = "Agua de Mesa", ImageUrl = "/images/agua-630ml.jpg", CreatedAt = DateTime.UtcNow },
                new() { Name = "Agua de Mesa botellón de 20 litros", Description = "1 unidad. Agua tratada y purificada con la más alta tecnología.", Price = 12.00m, Stock = 100, Category = "Agua de Mesa", ImageUrl = "/images/agua-botellon-20l.jpg", CreatedAt = DateTime.UtcNow },
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();
            Console.WriteLine("Semilla de productos TropiValle cargada.");
        }
    }
}
