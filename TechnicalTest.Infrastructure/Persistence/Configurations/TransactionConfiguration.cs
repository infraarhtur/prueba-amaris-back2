using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.SubscriptionId)
            .HasColumnName("subscription_id");

        builder.Property(transaction => transaction.ProductId)
            .HasColumnName("product_id");

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)");

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(transaction => transaction.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(transaction => transaction.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

