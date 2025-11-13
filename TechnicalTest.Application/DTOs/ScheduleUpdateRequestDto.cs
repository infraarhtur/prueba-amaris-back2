namespace TechnicalTest.Application.DTOs;

public record ScheduleUpdateRequestDto(int BankBranchId, Guid ClientId, DateTime AppointmentDate);


