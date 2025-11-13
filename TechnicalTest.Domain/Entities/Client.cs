using System.Linq;
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
        Email = string.Empty;
        Phone = string.Empty;
        NotificationChannel = NotificationChannel.Email;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Client(
        Guid id,
        Guid userId,
        string firstName,
        string lastName,
        string city,
        string email,
        string phone,
        decimal balance = InitialBalance,
        NotificationChannel notificationChannel = NotificationChannel.Email,
        DateTime? createdAtUtc = null)
    {
        Id = id;
        AssignUser(userId);
        UpdatePersonalInfo(firstName, lastName, city, email, phone);
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
    public string Email { get; private set; }
    public string Phone { get; private set; }
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

    public void UpdatePersonalInfo(string firstName, string lastName, string city, string email, string phone)
    {
        FirstName = NormalizeRequiredText(firstName, nameof(firstName));
        LastName = NormalizeRequiredText(lastName, nameof(lastName));
        City = NormalizeRequiredText(city, nameof(city));
        Email = ValidateAndNormalizeEmail(email);
        Phone = ValidateAndNormalizePhone(phone);
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

    private static string ValidateAndNormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("El campo email es requerido.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@') || normalizedEmail.Length < 5)
        {
            throw new DomainException("El formato del email no es válido.");
        }

        return normalizedEmail;
    }

    private static string ValidateAndNormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("El campo celular es requerido.");
        }

        // Remover espacios, guiones, paréntesis y otros caracteres comunes
        var normalizedPhone = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Validar que tenga al menos 7 dígitos (número mínimo razonable para un teléfono)
        if (normalizedPhone.Length < 7 || normalizedPhone.Length > 15)
        {
            throw new DomainException("El formato del celular no es válido. Debe tener entre 7 y 15 dígitos.");
        }

        return normalizedPhone;
    }
}

