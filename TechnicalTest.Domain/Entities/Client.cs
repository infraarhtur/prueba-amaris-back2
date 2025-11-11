using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Client
{
    public const decimal InitialBalance = 500_000m;

    public Client(
        Guid id,
        decimal balance = InitialBalance,
        NotificationChannel notificationChannel = NotificationChannel.Email,
        DateTime? createdAtUtc = null)
    {
        if (balance < 0)
        {
            throw new DomainException("El saldo inicial del cliente no puede ser negativo.");
        }

        Id = id;
        Balance = balance;
        NotificationChannel = notificationChannel;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; }
    public decimal Balance { get; private set; }
    public NotificationChannel NotificationChannel { get; private set; }
    public DateTime CreatedAtUtc { get; private init; }

    public void Debit(decimal amount, string? fundName = null)
    {
        ValidateAmount(amount);

        if (Balance < amount)
        {
            var message = fundName is null
                ? "No tiene saldo disponible para realizar la transacción."
                : $"No tiene saldo disponible para vincularse al fondo {fundName}.";

            throw new DomainException(message);
        }

        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        ValidateAmount(amount);
        Balance += amount;
    }

    public void UpdateNotificationChannel(NotificationChannel channel)
    {
        NotificationChannel = channel;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("El monto debe ser mayor a cero.");
        }
    }
}

