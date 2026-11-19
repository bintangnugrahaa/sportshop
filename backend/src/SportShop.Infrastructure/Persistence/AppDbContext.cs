using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportShop.Domain.Carts;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Domain.Orders;
using SportShop.Domain.Shipping;
using SportShop.Domain.Users;
using SportShop.Infrastructure.Identity;

namespace SportShop.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("pg_trgm");
        builder.HasPostgresExtension("unaccent");

        builder.Entity<ApplicationUser>(b => b.ToTable("users"));
        builder.Entity<ApplicationRole>(b => b.ToTable("roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
