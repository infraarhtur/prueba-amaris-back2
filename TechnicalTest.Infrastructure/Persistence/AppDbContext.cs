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
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new FundConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        var seedClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var seedClient = new
        {
            Id = seedClientId,
            Balance = Client.InitialBalance,
            NotificationChannel = NotificationChannel.Email,
            CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var seedFunds = FundCatalog.GetDefaultFunds()
            .Select(fund => new
            {
                fund.Id,
                fund.Name,
                fund.MinimumAmount,
                fund.Category
            });

        modelBuilder.Entity<Client>().HasData(seedClient);
        modelBuilder.Entity<Fund>().HasData(seedFunds);
    }
}

