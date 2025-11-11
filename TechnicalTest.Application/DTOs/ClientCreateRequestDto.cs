namespace TechnicalTest.Application.DTOs;

public record ClientCreateRequestDto(
    string FirstName,
    string LastName,
    string City,
    decimal? Balance = null,
    string? NotificationChannel = null);
