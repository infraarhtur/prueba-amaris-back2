using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(subscription => subscription.ClientId)
            .HasColumnName("client_id");

        builder.Property(subscription => subscription.FundId)
            .HasColumnName("fund_id");

        builder.Property(subscription => subscription.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)");

        builder.Property(subscription => subscription.SubscribedAtUtc)
            .HasColumnName("subscribed_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(subscription => subscription.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Ignore(subscription => subscription.IsActive);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(subscription => subscription.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Fund>()
            .WithMany()
            .HasForeignKey(subscription => subscription.FundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

