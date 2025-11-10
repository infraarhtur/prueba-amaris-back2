namespace BTGPactual.Fondos.Application.DTOs;

public record ClientBalanceDto(Guid ClientId, decimal Balance, string NotificationChannel);

