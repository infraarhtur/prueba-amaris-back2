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

    public static ClientBalanceDto ToDto(this Client client) =>
        new(client.Id, client.Balance, client.NotificationChannel.ToString());

    public static ClientDto ToClientDto(this Client client) =>
        new(
            client.Id,
            client.UserId,
            client.FirstName,
            client.LastName,
            client.City,
            client.Email,
            client.Balance,
            client.NotificationChannel.ToString(),
            client.CreatedAtUtc);

    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Email, user.FullName);

    public static BankBranchDto ToDto(this BankBranch bankBranch) =>
        new(bankBranch.Id, bankBranch.Name, bankBranch.City);

    public static AvailabilityDto ToDto(this Availability availability) =>
        new(availability.Id, availability.BankBranchId, availability.ProductId);

    public static ScheduleDto ToDto(this Schedule schedule) =>
        new(schedule.Id, schedule.BankBranchId, schedule.ClientId, schedule.AppointmentDate);

    public static NotificationChannel ParseChannel(string channel) =>
        Enum.TryParse<NotificationChannel>(channel, true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Canal de notificación desconocido: {channel}", nameof(channel));

    public static NotificationChannel ParseChannelOrDefault(string? channel, NotificationChannel defaultChannel) =>
        string.IsNullOrWhiteSpace(channel) ? defaultChannel : ParseChannel(channel);
}

