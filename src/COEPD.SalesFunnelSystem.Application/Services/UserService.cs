using COEPD.SalesFunnelSystem.Application.Common;
using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;
using COEPD.SalesFunnelSystem.Domain.Enums;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private const int PasswordWorkFactor = 12;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await _userRepository.GetAllAsync(cancellationToken))
        .OrderBy(x => x.FullName)
        .Select(MapToResponse)
        .ToList();

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with the same email already exists.");
        }

        var role = NormalizeRole(request.Role);
        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, PasswordWorkFactor),
            Role = role,
            IsActive = request.IsActive
        };

        var created = await _userRepository.AddAsync(user, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<UserResponse> UpdateRoleAsync(int userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found.");
        user.Role = NormalizeRole(request.Role);
        await _userRepository.UpdateAsync(user, cancellationToken);
        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateStatusAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found.");
        user.IsActive = request.IsActive;
        await _userRepository.UpdateAsync(user, cancellationToken);
        return MapToResponse(user);
    }

    private static string NormalizeRole(string? role)
    {
        var value = role?.Trim();
        return value?.ToLowerInvariant() switch
        {
            "admin" => UserRole.Admin,
            "staff" => UserRole.Staff,
            _ => throw new ArgumentException("Invalid role. Allowed values: Admin, Staff.")
        };
    }

    private static UserResponse MapToResponse(AppUser user) =>
        new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
}
