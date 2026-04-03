using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Enums;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int PasswordWorkFactor = 12;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailForUpdateAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive || !IsSupportedRole(user.Role))
        {
            return null;
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            return null;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;

        if (BCrypt.Net.BCrypt.PasswordNeedsRehash(user.PasswordHash, PasswordWorkFactor))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, PasswordWorkFactor);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            Token = _tokenService.GenerateToken(user),
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }

    private static bool IsSupportedRole(string role) =>
        role is UserRole.Admin or UserRole.Staff;
}
