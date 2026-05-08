using System.ComponentModel.DataAnnotations;

namespace OfflinePaymentLinks.API.Models.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record RegisterRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Phone] string? Phone
);

public record AuthResponse(
    string Token,
    UserDto User
);

public record UserDto(
    string Id,
    string Email,
    string Name,
    string Role,
    bool IsApproved
);

public record ApproveUserRequest(
    [Required] string UserId,
    [Required] string RoleName
);

public record CreateAdminRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);

public record UpdateUserRoleRequest(    
    [Required] string NewRole
);

public record RejectUserRequest([Required] string UserId);