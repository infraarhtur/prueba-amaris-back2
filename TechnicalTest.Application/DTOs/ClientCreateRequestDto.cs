namespace TechnicalTest.Application.DTOs;

public record ClientCreateRequestDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string City,
    string Email,
    string Phone,
    decimal? Balance = null,
    string? NotificationChannel = null);
