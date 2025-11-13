using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class BankBranchConfiguration : IEntityTypeConfiguration<BankBranch>
{
    public void Configure(EntityTypeBuilder<BankBranch> builder)
    {
        builder.ToTable("bank_branches");

        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(branch => branch.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(branch => branch.City)
            .HasColumnName("city")
            .IsRequired()
            .HasMaxLength(120);
    }
}


