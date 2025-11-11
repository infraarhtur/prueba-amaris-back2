using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("availability");

        builder.HasKey(availability => availability.Id);

        builder.Property(availability => availability.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(availability => availability.BankBranchId)
            .HasColumnName("id_bank_branch")
            .IsRequired();

        builder.Property(availability => availability.ProductId)
            .HasColumnName("id_product")
            .IsRequired();

        builder.HasIndex(availability => new { availability.BankBranchId, availability.ProductId })
            .IsUnique()
            .HasDatabaseName("ux_availability_branch_product");

        builder.HasOne<BankBranch>()
            .WithMany()
            .HasForeignKey(availability => availability.BankBranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(availability => availability.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}



