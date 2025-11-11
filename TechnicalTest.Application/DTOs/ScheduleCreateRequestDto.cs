namespace TechnicalTest.Application.DTOs;

public record ScheduleCreateRequestDto(int BankBranchId, Guid ClientId, DateTime AppointmentDate);


