using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Interfaces.Security;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        TimeProvider? timeProvider = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new DomainException("El correo electrónico es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException("La contraseña es obligatoria.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);

        if (existingUser is not null)
        {
            throw new DomainException("El correo electrónico ya se encuentra registrado.");
        }

        var hashResult = _passwordHasher.HashPassword(request.Password);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var user = new User(Guid.NewGuid(), normalizedEmail, hashResult.Hash, hashResult.Salt, request.FullName, nowUtc);

        await _userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);

        var token = _jwtProvider.GenerateToken(user);
        return new AuthResponseDto(token, user.ToDto());
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException("Credenciales inválidas.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false)
                   ?? throw new DomainException("Credenciales inválidas.");

        var passwordIsValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!passwordIsValid)
        {
            throw new DomainException("Credenciales inválidas.");
        }

        var token = _jwtProvider.GenerateToken(user);
        return new AuthResponseDto(token, user.ToDto());
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
                   ?? throw new DomainException("No se encontró el usuario solicitado.");

        return user.ToDto();
    }
}


