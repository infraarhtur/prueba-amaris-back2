using System.ComponentModel.DataAnnotations;

namespace BTGPactual.Fondos.Application.DTOs;

public record SubscriptionRequestDto(
    [property: Required] int FundId,
    [property: Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")] decimal Amount,
    [property: Required] string NotificationChannel);

