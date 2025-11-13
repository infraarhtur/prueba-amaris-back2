namespace TechnicalTest.Application.DTOs;

public record ScheduleDto(int Id, int BankBranchId, Guid ClientId, DateTime AppointmentDate);


