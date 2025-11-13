namespace TechnicalTest.Application.DTOs;

public record ClientDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string City,
    string Email,
    decimal Balance,
    string NotificationChannel,
    DateTime CreatedAtUtc);
