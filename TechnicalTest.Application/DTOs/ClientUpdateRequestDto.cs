namespace TechnicalTest.Application.DTOs;

public record ClientUpdateRequestDto(
    string FirstName,
    string LastName,
    string City,
    decimal? Balance = null,
    string? NotificationChannel = null);
