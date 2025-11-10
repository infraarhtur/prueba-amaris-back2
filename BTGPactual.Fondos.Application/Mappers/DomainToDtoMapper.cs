using BTGPactual.Fondos.Application.DTOs;
using BTGPactual.Fondos.Domain.Entities;
using BTGPactual.Fondos.Domain.Enums;

namespace BTGPactual.Fondos.Application.Mappers;

public static class DomainToDtoMapper
{
    public static FundDto ToDto(this Fund fund) =>
        new(fund.Id, fund.Name, fund.MinimumAmount, fund.Category.ToString());

    public static SubscriptionDto ToDto(this Subscription subscription) =>
        new(subscription.Id, subscription.FundId, subscription.Amount, subscription.SubscribedAtUtc, subscription.IsActive);

    public static TransactionDto ToDto(this Transaction transaction) =>
        new(transaction.Id, transaction.SubscriptionId, transaction.FundId, transaction.Amount, transaction.Type.ToString(), transaction.OccurredAtUtc);

    public static ClientBalanceDto ToDto(this Client client) =>
        new(client.Id, client.Balance, client.NotificationChannel.ToString());

    public static NotificationChannel ParseChannel(string channel) =>
        Enum.TryParse<NotificationChannel>(channel, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Canal de notificación desconocido: {channel}", nameof(channel));
}

