using Microsoft.EntityFrameworkCore;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Infrastructure.Persistence.Configurations;

namespace TechnicalTest.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new FundConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
    }
}

