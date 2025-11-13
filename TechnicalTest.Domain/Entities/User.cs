using System.Text.RegularExpressions;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class User
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        PasswordSalt = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        string passwordSalt,
        string? fullName = null,
        DateTime? createdAtUtc = null)
    {
        if (!IsValidEmail(email))
        {
            throw new DomainException("El correo electrónico no es válido.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
        {
            throw new DomainException("La contraseña es obligatoria.");
        }

        Id = id;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        FullName = fullName?.Trim();
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }
    public string? FullName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public void UpdatePassword(string passwordHash, string passwordSalt)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
        {
            throw new DomainException("La contraseña es obligatoria.");
        }

        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
    }

    public void UpdateFullName(string? fullName)
    {
        FullName = fullName?.Trim();
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
    }
}


