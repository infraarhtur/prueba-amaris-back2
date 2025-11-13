namespace TechnicalTest.Application.Interfaces.Security;

public interface IPasswordHasher
{
    PasswordHashResult HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash, string passwordSalt);
}

public sealed record PasswordHashResult(string Hash, string Salt);


