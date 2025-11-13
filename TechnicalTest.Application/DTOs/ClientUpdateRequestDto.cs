namespace TechnicalTest.Application.DTOs;

public record ClientUpdateRequestDto(
    string FirstName,
    string LastName,
    string City,
    string Email,
    string Phone,
    decimal? Balance = null,
    string? NotificationChannel = null,
    Guid? UserId = null);
