using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Application.DTOs;

public record SubscriptionRequestDto(
    [property: Required] int ProductId,
    [property: Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")] decimal Amount,
    [property: Required] string NotificationChannel);

