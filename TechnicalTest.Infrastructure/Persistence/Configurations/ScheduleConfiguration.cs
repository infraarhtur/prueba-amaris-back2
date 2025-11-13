using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules");

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(schedule => schedule.BankBranchId)
            .HasColumnName("id_bank_branch")
            .IsRequired();

        builder.Property(schedule => schedule.ClientId)
            .HasColumnName("id_client")
            .IsRequired();

        builder.Property(schedule => schedule.AppointmentDate)
            .HasColumnName("appointment_date")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<BankBranch>()
            .WithMany()
            .HasForeignKey(schedule => schedule.BankBranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(schedule => schedule.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(schedule => new { schedule.BankBranchId, schedule.ClientId, schedule.AppointmentDate })
            .HasDatabaseName("ux_schedule_branch_client_date")
            .IsUnique();
    }
}


