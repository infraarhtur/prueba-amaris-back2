using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(client => client.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(client => client.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(client => client.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(client => client.City)
            .HasColumnName("city")
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(client => client.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(client => client.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,2)");

        builder.Property(client => client.NotificationChannel)
            .HasColumnName("notification_channel")
            .HasConversion<int>();

        builder.Property(client => client.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasOne(client => client.User)
            .WithMany()
            .HasForeignKey(client => client.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

