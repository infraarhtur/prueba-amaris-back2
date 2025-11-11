using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("funds");

        builder.HasKey(fund => fund.Id);

        builder.Property(fund => fund.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(fund => fund.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(fund => fund.MinimumAmount)
            .HasColumnName("minimum_amount")
            .HasColumnType("numeric(18,2)");

        builder.Property(fund => fund.Category)
            .HasColumnName("category")
            .HasConversion<int>();
    }
}

