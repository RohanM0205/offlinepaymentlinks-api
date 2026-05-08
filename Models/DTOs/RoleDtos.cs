using System.ComponentModel.DataAnnotations;

namespace OfflinePaymentLinks.API.Models.DTOs;

public record RoleWithPermissionsDto(
    string Id,
    string Name,
    bool IsSystem,
    RolePermissionDto? Permissions
);

public record RolePermissionDto(
    bool CanSendPaymentLink,
    bool CanViewReports,
    bool CanManageUsers,
    bool CanApproveUsers,
    bool CanManageRoles,
    bool CanManageAdmins,
    List<string> AllowedTransactionTypes
);

public record CreateRoleRequest(
    [Required, MaxLength(100)] string Name
);

public record RenameRoleRequest(
    [Required, MaxLength(100)] string NewName
);

public record UpdateRolePermissionsRequest(
    bool CanSendPaymentLink,
    bool CanViewReports,
    bool CanManageUsers,
    bool CanApproveUsers,
    bool CanManageRoles,
    bool CanManageAdmins,
    List<string> AllowedTransactionTypes
);