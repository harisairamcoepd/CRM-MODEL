namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
