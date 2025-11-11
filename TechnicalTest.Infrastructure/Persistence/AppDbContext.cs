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
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new BankBranchConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new AvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        var seedClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var seedClient = new
        {
            Id = seedClientId,
            FirstName = "Demo",
            LastName = "Client",
            City = "Bogota",
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

        modelBuilder.Entity<Client>().HasData(seedClient);
        modelBuilder.Entity<Product>().HasData(seedProducts);
    }
}

