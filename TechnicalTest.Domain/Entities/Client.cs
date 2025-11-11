using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Client
{
    public const decimal InitialBalance = 500_000m;

    private Client()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        City = string.Empty;
        NotificationChannel = NotificationChannel.Email;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Client(
        Guid id,
        Guid userId,
        string firstName,
        string lastName,
        string city,
        decimal balance = InitialBalance,
        NotificationChannel notificationChannel = NotificationChannel.Email,
        DateTime? createdAtUtc = null)
    {
        Id = id;
        AssignUser(userId);
        UpdatePersonalInfo(firstName, lastName, city);
        UpdateBalance(balance);
        NotificationChannel = notificationChannel;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string City { get; private set; }
    public decimal Balance { get; private set; }
    public NotificationChannel NotificationChannel { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void Debit(decimal amount, string? productName = null)
    {
        ValidateAmount(amount);

        if (Balance < amount)
        {
            var message = productName is null
                ? "No tiene saldo disponible para realizar la transacción."
                : $"No tiene saldo disponible para vincularse al producto {productName}.";

            throw new DomainException(message);
        }

        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        ValidateAmount(amount);
        Balance += amount;
    }

    public void UpdatePersonalInfo(string firstName, string lastName, string city)
    {
        FirstName = NormalizeRequiredText(firstName, nameof(firstName));
        LastName = NormalizeRequiredText(lastName, nameof(lastName));
        City = NormalizeRequiredText(city, nameof(city));
    }

    public void UpdateNotificationChannel(NotificationChannel channel)
    {
        NotificationChannel = channel;
    }

    public void AssignUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("El identificador de usuario es obligatorio.");
        }

        UserId = userId;
    }

    public void UpdateBalance(decimal balance)
    {
        if (balance < 0)
        {
            throw new DomainException("El saldo del cliente no puede ser negativo.");
        }

        Balance = balance;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("El monto debe ser mayor a cero.");
        }
    }

    private static string NormalizeRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"El campo {fieldName} es requerido.");
        }

        return value.Trim();
    }
}

