using FluentAssertions;
using TechnicalTest.Infrastructure.Authentication;

namespace TechnicalTest.Tests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ShouldReturnHashAndSalt()
    {
        var result = _sut.HashPassword("Sup3rS3cret!");

        result.Hash.Should().NotBeNullOrWhiteSpace();
        result.Salt.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(result.Hash).Length.Should().BeGreaterThan(0);
        Convert.FromBase64String(result.Salt).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentSaltEachTime()
    {
        var first = _sut.HashPassword("Sup3rS3cret!");
        var second = _sut.HashPassword("Sup3rS3cret!");

        second.Salt.Should().NotBe(first.Salt);
        second.Hash.Should().NotBe(first.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_ShouldThrow_WhenPasswordIsNullOrWhitespace(string input)
    {
        Action act = () => _sut.HashPassword(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_ForMatchingPassword()
    {
        var password = "Sup3rS3cret!";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword(password, hash.Hash, hash.Salt);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForMismatchedPassword()
    {
        var hash = _sut.HashPassword("Sup3rS3cret!");

        var result = _sut.VerifyPassword("wrong-password", hash.Hash, hash.Salt);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void VerifyPassword_ShouldReturnFalse_WhenHashOrSaltMissing(string? input)
    {
        var hash = _sut.HashPassword("Sup3rS3cret!");

        _sut.VerifyPassword("Sup3rS3cret!", input!, hash.Salt).Should().BeFalse();
        _sut.VerifyPassword("Sup3rS3cret!", hash.Hash, input!).Should().BeFalse();
    }
}

