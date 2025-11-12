using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Interfaces.Security;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtProvider> _jwtProvider = new();
    private readonly TimeProvider _timeProvider = new FixedTimeProvider(new DateTimeOffset(2024, 5, 12, 8, 30, 0, TimeSpan.Zero));

    private AuthService CreateSut() =>
        new(_userRepository.Object, _passwordHasher.Object, _jwtProvider.Object, _timeProvider);

    [Fact]
    public async Task RegisterAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.RegisterAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new RegisterRequestDto("user@example.com", "Password123!", "Test User");

        var act = async () => await sut.RegisterAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowDomainException_WhenEmailIsMissing()
    {
        var sut = CreateSut();
        var request = new RegisterRequestDto(" ", "Password123!", "Test User");

        var act = async () => await sut.RegisterAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El correo electrónico es obligatorio.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowDomainException_WhenPasswordIsMissing()
    {
        var sut = CreateSut();
        var request = new RegisterRequestDto("user@example.com", "", "Test User");

        var act = async () => await sut.RegisterAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("La contraseña es obligatoria.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowDomainException_WhenEmailAlreadyExists()
    {
        var sut = CreateSut();
        var request = new RegisterRequestDto("user@example.com", "Password123!", "Test User");
        var existingUser = CreateUser(Guid.NewGuid(), request.Email);
        _userRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var act = async () => await sut.RegisterAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El correo electrónico ya se encuentra registrado.");
        _userRepository.Verify(
            repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnAuthResponse_WhenRegistrationSucceeds()
    {
        var sut = CreateSut();
        var rawEmail = "  USER@example.COM ";
        var normalizedEmail = "user@example.com";
        var request = new RegisterRequestDto(rawEmail, "Password123!", " Test User ");
        var passwordHash = new PasswordHashResult("hashed-password", "salt-value");
        User? addedUser = null;

        _userRepository
            .Setup(repo => repo.GetByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher
            .Setup(hasher => hasher.HashPassword(request.Password))
            .Returns(passwordHash);
        _userRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);
        _jwtProvider
            .Setup(provider => provider.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token");

        var result = await sut.RegisterAsync(request);

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be(normalizedEmail);
        result.User.FullName.Should().Be("Test User");
        addedUser.Should().NotBeNull();
        addedUser!.Email.Should().Be(normalizedEmail);
        addedUser.FullName.Should().Be("Test User");
        addedUser.PasswordHash.Should().Be(passwordHash.Hash);
        addedUser.PasswordSalt.Should().Be(passwordHash.Salt);
        addedUser.CreatedAtUtc.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
        _jwtProvider.Verify(provider => provider.GenerateToken(addedUser), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.LoginAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new LoginRequestDto("user@example.com", "Password123!");

        var act = async () => await sut.LoginAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(null, "Password123!")]
    [InlineData("", "Password123!")]
    [InlineData("user@example.com", null)]
    [InlineData("user@example.com", "")]
    public async Task LoginAsync_ShouldThrowDomainException_WhenCredentialsAreMissing(string? email, string? password)
    {
        var sut = CreateSut();
        var request = new LoginRequestDto(email!, password!);

        var act = async () => await sut.LoginAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowDomainException_WhenUserDoesNotExist()
    {
        var sut = CreateSut();
        var request = new LoginRequestDto("user@example.com", "Password123!");
        _userRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await sut.LoginAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowDomainException_WhenPasswordIsInvalid()
    {
        var sut = CreateSut();
        var request = new LoginRequestDto("user@example.com", "wrong");
        var user = CreateUser(Guid.NewGuid(), request.Email);
        _userRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(hasher => hasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(false);

        var act = async () => await sut.LoginAsync(request);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        var sut = CreateSut();
        var rawEmail = " USER@example.com ";
        var normalizedEmail = "user@example.com";
        var request = new LoginRequestDto(rawEmail, "Password123!");
        var user = CreateUser(Guid.NewGuid(), normalizedEmail);

        _userRepository
            .Setup(repo => repo.GetByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher
            .Setup(hasher => hasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(true);
        _jwtProvider
            .Setup(provider => provider.GenerateToken(user))
            .Returns("jwt-token");

        var result = await sut.LoginAsync(request);

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be(normalizedEmail);
        _jwtProvider.Verify(provider => provider.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetProfileAsync(Guid.NewGuid(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowDomainException_WhenUserDoesNotExist()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        _userRepository
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await sut.GetProfileAsync(userId);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró el usuario solicitado.");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnUserDto_WhenUserExists()
    {
        var sut = CreateSut();
        var user = CreateUser(Guid.NewGuid(), "user@example.com", fullName: "Test User");
        _userRepository
            .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await sut.GetProfileAsync(user.Id);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
    }

    private static User CreateUser(Guid id, string email, string passwordHash = "hash", string passwordSalt = "salt", string? fullName = null) =>
        new(id, email, passwordHash, passwordSalt, fullName);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}

