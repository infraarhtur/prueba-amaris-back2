using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TechnicalTest.Application.Interfaces.Security;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Infrastructure.Authentication;

namespace TechnicalTest.Tests.Infrastructure;

public class JwtProviderTests
{
    private static JwtSettings CreateSettings() =>
        new()
        {
            Issuer = "TechnicalTest.Issuer",
            Audience = "TechnicalTest.Audience",
            Key = "ThisIsASufficientlyLongJwtSigningKey!",
            ExpirationMinutes = 45
        };

    private static User CreateUser(string? fullName = "Demo User") =>
        new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000"), "demo@amaris.com", "hash", "salt", fullName);

    [Fact]
    public void Ctor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        Action act = () => _ = new JwtProvider(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("options");
    }

    [Fact]
    public void Ctor_ShouldThrowArgumentNullException_WhenOptionsValueIsNull()
    {
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.SetupGet(opt => opt.Value).Returns((JwtSettings)null!);

        Action act = () => _ = new JwtProvider(optionsMock.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_ShouldThrowInvalidOperationException_WhenKeyMissing(string key)
    {
        var settings = new JwtSettings
        {
            Issuer = "TechnicalTest.Issuer",
            Audience = "TechnicalTest.Audience",
            Key = key,
            ExpirationMinutes = 45
        };
        var options = Options.Create(settings);

        Action act = () => _ = new JwtProvider(options);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("La clave de firma JWT no está configurada.");
    }

    [Fact]
    public void GenerateToken_ShouldThrowArgumentNullException_WhenUserIsNull()
    {
        var sut = new JwtProvider(Options.Create(CreateSettings()));

        Action act = () => sut.GenerateToken(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("user");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeExpectedClaims()
    {
        var settings = CreateSettings();
        var now = new DateTimeOffset(2024, 3, 15, 8, 30, 0, TimeSpan.Zero);
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(tp => tp.GetUtcNow()).Returns(now);
        var sut = new JwtProvider(Options.Create(settings), timeProvider.Object);
        var user = CreateUser();

        var tokenString = sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        token.Issuer.Should().Be(settings.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(settings.Audience);
        token.ValidFrom.Should().Be(now.UtcDateTime);
        token.ValidTo.Should().Be(now.UtcDateTime.AddMinutes(settings.ExpirationMinutes));

        var claims = token.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);
        claims.Should().ContainKey(JwtRegisteredClaimNames.Sub).WhoseValue.Should().Be(user.Id.ToString());
        claims.Should().ContainKey(JwtRegisteredClaimNames.Email).WhoseValue.Should().Be(user.Email);
        claims.Should().ContainKey(ClaimTypes.NameIdentifier).WhoseValue.Should().Be(user.Id.ToString());
        claims.Should().ContainKey(JwtRegisteredClaimNames.Name).WhoseValue.Should().Be(user.FullName);
    }

    [Fact]
    public void GenerateToken_ShouldOmitNameClaim_WhenFullNameIsNotProvided()
    {
        var settings = CreateSettings();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(tp => tp.GetUtcNow()).Returns(now);
        var sut = new JwtProvider(Options.Create(settings), timeProvider.Object);
        var user = CreateUser(fullName: null);

        var tokenString = sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        token.Claims.Select(claim => claim.Type)
            .Should()
            .NotContain(JwtRegisteredClaimNames.Name);
    }
}

