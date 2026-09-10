using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Usuario administrador opcional. Se crea solo si configurás estas variables
        // de entorno: ADMIN_EMAIL y ADMIN_PASSWORD.
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword))
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

        // Semilla de productos Tropivalle (jugos naturales)
        if (!await db.Products.AnyAsync())
        {
            var products = new List<Product>
            {
                new()
                {
                    Name = "Jugo de Naranja Natural",
                    Description = "Naranjas frescas del valle, exprimidas al momento. Sin azúcar añadida ni conservantes.",
                    Price = 2.50m,
                    Stock = 120,
                    Category = "Individual",
                    ImageUrl = "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Naranja Natural — Pack x12",
                    Description = "Caja de 12 botellas de jugo de naranja 100% natural. Ideal para hogar u oficina.",
                    Price = 26.00m,
                    Stock = 40,
                    Category = "Pack x12",
                    ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo Verde Detox",
                    Description = "Mezcla de espinaca, manzana verde, pepino y limón. Energía y frescura en cada sorbo.",
                    Price = 3.20m,
                    Stock = 80,
                    Category = "Individual",
                    ImageUrl = "https://images.unsplash.com/photo-1610970881699-44a5587cabec?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo Verde Detox — Pack x12",
                    Description = "Paquete de 12 botellas de jugo verde detox. Perfecto para tu rutina semanal de bienestar.",
                    Price = 34.00m,
                    Stock = 25,
                    Category = "Pack x12",
                    ImageUrl = "https://images.unsplash.com/photo-1622597467836-f3285f2131b8?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Mango Tropical",
                    Description = "Mango maduro del valle, cremoso y dulce de forma natural. Sin aditivos.",
                    Price = 2.80m,
                    Stock = 95,
                    Category = "Individual",
                    ImageUrl = "https://images.unsplash.com/photo-1546173159-315724a31696?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Mango Tropical — Pack x12",
                    Description = "Caja de 12 botellas de jugo de mango tropical. Sabor intenso y refrescante.",
                    Price = 29.50m,
                    Stock = 30,
                    Category = "Pack x12",
                    ImageUrl = "https://images.unsplash.com/photo-1623065422902-30a2d299bbe4?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Piña y Hierbabuena",
                    Description = "Piña jugosa con un toque de hierbabuena fresca. Ideal para hidratarse con sabor.",
                    Price = 2.70m,
                    Stock = 70,
                    Category = "Individual",
                    ImageUrl = "https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Piña y Hierbabuena — Pack x12",
                    Description = "Paquete de 12 botellas de piña con hierbabuena. Frescura tropical para toda la semana.",
                    Price = 28.00m,
                    Stock = 22,
                    Category = "Pack x12",
                    ImageUrl = "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Fresa Natural",
                    Description = "Fresas seleccionadas, suaves y aromáticas. Solo fruta, nada más.",
                    Price = 3.00m,
                    Stock = 60,
                    Category = "Individual",
                    ImageUrl = "https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Jugo de Fresa Natural — Pack x12",
                    Description = "Caja de 12 botellas de jugo de fresa 100% natural. Dulzura auténtica del valle.",
                    Price = 32.00m,
                    Stock = 18,
                    Category = "Pack x12",
                    ImageUrl = "https://images.unsplash.com/photo-1464454709131-ffd692591ee5?w=800&q=80",
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }
    }
}
