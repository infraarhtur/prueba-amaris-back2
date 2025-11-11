namespace TechnicalTest.Application.DTOs;

public record ClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string City,
    decimal Balance,
    string NotificationChannel,
    DateTime CreatedAtUtc);
