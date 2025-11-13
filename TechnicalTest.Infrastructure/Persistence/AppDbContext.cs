using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Domain.Data;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Infrastructure.Persistence.Configurations;

namespace TechnicalTest.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<BankBranch> BankBranches => Set<BankBranch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new BankBranchConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());

        var seedUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var seedClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var seedUser = new
        {
            Id = seedUserId,
            Email = "demo.client@amaris.com",
            PasswordHash = "ZGVmYXVsdF9wYXNzd29yZF9oYXNo",
            PasswordSalt = "ZGVmYXVsdF9wYXNzd29yZF9zYWx0",
            FullName = "Demo Client",
            CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var seedClient = new
        {
            Id = seedClientId,
            UserId = seedUserId,
            FirstName = "Demo",
            LastName = "Client",
            City = "Bogota",
            Email = "demo.client@amaris.com",
            Balance = Client.InitialBalance,
            NotificationChannel = NotificationChannel.Email,
            CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var seedProducts = ProductCatalog.GetDefaultProducts()
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.MinimumAmount,
                product.Category
            });

        modelBuilder.Entity<User>().HasData(seedUser);
        modelBuilder.Entity<Client>().HasData(seedClient);
        modelBuilder.Entity<Product>().HasData(seedProducts);
    }
}

