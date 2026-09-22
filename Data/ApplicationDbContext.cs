using EcommerceApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Sale>(e =>
        {
            e.Property(s => s.UnitPrice).HasPrecision(18, 2);
            e.Property(s => s.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(s => s.SoldAt);
            e.HasIndex(s => s.Category);
            // Sin FK en BD: ProductId es solo referencia informativa
            e.Ignore(s => s.Product);
        });
    }
}
