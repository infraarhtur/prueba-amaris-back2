using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Security;

public interface IJwtProvider
{
    string GenerateToken(User user);
}


