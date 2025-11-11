using TechnicalTest.Application.DTOs;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Application.Mappers;

public static class DomainToDtoMapper
{
    public static ProductDto ToDto(this Product product) =>
        new(product.Id, product.Name, product.MinimumAmount, product.Category.ToString());

    public static SubscriptionDto ToDto(this Subscription subscription) =>
        new(
            subscription.Id,
            subscription.ClientId,
            subscription.ProductId,
            subscription.Amount,
            subscription.SubscribedAtUtc,
            subscription.CancelledAtUtc,
            subscription.IsActive);

    public static TransactionDto ToDto(this Transaction transaction) =>
        new(transaction.Id, transaction.SubscriptionId, transaction.ProductId, transaction.Amount, transaction.Type.ToString(), transaction.OccurredAtUtc);

    public static ClientBalanceDto ToDto(this Client client) =>
        new(client.Id, client.Balance, client.NotificationChannel.ToString());

    public static ClientDto ToClientDto(this Client client) =>
        new(
            client.Id,
            client.FirstName,
            client.LastName,
            client.City,
            client.Balance,
            client.NotificationChannel.ToString(),
            client.CreatedAtUtc);

    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Email, user.FullName);

    public static BankBranchDto ToDto(this BankBranch bankBranch) =>
        new(bankBranch.Id, bankBranch.Name, bankBranch.City);

    public static NotificationChannel ParseChannel(string channel) =>
        Enum.TryParse<NotificationChannel>(channel, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Canal de notificación desconocido: {channel}", nameof(channel));

    public static NotificationChannel ParseChannelOrDefault(string? channel, NotificationChannel defaultChannel) =>
        string.IsNullOrWhiteSpace(channel) ? defaultChannel : ParseChannel(channel);
}

