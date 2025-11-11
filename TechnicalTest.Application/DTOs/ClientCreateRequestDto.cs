namespace TechnicalTest.Application.DTOs;

public record ClientCreateRequestDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string City,
    decimal? Balance = null,
    string? NotificationChannel = null);
