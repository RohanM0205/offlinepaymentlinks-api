using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Models;
using OfflinePaymentLinks.API.Models.DTOs;

namespace OfflinePaymentLinks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    private static readonly string[] SystemRoles = ["SuperAdmin", "Admin"];

    public RolesController(RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var permissions = await _context.RolePermissions.ToListAsync();

        var result = roles.Select(r =>
        {
            var perm = permissions.FirstOrDefault(p => p.RoleId == r.Id);
            return new RoleWithPermissionsDto(
                r.Id,
                r.Name!,
                SystemRoles.Contains(r.Name),
                perm == null ? null : new RolePermissionDto(
                    perm.CanSendPaymentLink,
                    perm.CanViewReports,
                    perm.CanManageUsers,
                    perm.CanApproveUsers,
                    perm.CanManageRoles,
                    perm.CanManageAdmins,
                    JsonSerializer.Deserialize<List<string>>(perm.AllowedTransactionTypes) ?? []
                )
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest req)
    {
        if (await _roleManager.RoleExistsAsync(req.Name))
            return Conflict(new { message = "Role already exists." });

        var role = new IdentityRole(req.Name);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        // Create default permissions entry
        _context.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            CanSendPaymentLink = false,
        });
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Role '{req.Name}' created.", id = role.Id, name = role.Name });
    }

    [HttpPut("{roleId}/rename")]
    public async Task<IActionResult> RenameRole(string roleId, [FromBody] RenameRoleRequest req)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound(new { message = "Role not found." });

        if (SystemRoles.Contains(role.Name))
            return BadRequest(new { message = "System roles cannot be renamed." });

        if (await _roleManager.RoleExistsAsync(req.NewName))
            return Conflict(new { message = "A role with that name already exists." });

        role.Name = req.NewName;
        await _roleManager.UpdateAsync(role);

        return Ok(new { message = "Role renamed successfully." });
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound(new { message = "Role not found." });

        if (SystemRoles.Contains(role.Name))
            return BadRequest(new { message = "System roles cannot be deleted." });

        // Remove permissions entry
        var perm = await _context.RolePermissions.FirstOrDefaultAsync(p => p.RoleId == roleId);
        if (perm != null) _context.RolePermissions.Remove(perm);

        await _roleManager.DeleteAsync(role);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Role '{role.Name}' deleted." });
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<IActionResult> UpdatePermissions(
        string roleId,
        [FromBody] UpdateRolePermissionsRequest req)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound(new { message = "Role not found." });

        var perm = await _context.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleId == roleId);

        if (perm == null)
        {
            perm = new RolePermission { RoleId = roleId };
            _context.RolePermissions.Add(perm);
        }

        perm.CanSendPaymentLink = req.CanSendPaymentLink;
        perm.CanViewReports = req.CanViewReports;
        perm.CanManageUsers = req.CanManageUsers;
        perm.CanApproveUsers = req.CanApproveUsers;
        perm.CanManageRoles = req.CanManageRoles;
        perm.CanManageAdmins = req.CanManageAdmins;
        perm.AllowedTransactionTypes = JsonSerializer.Serialize(req.AllowedTransactionTypes);
        perm.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Permissions updated." });
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetPermissions(string roleId)
    {
        var perm = await _context.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleId == roleId);

        if (perm == null)
            return Ok(new RolePermissionDto(false, false, false, false, false, false, []));

        return Ok(new RolePermissionDto(
            perm.CanSendPaymentLink,
            perm.CanViewReports,
            perm.CanManageUsers,
            perm.CanApproveUsers,
            perm.CanManageRoles,
            perm.CanManageAdmins,
            JsonSerializer.Deserialize<List<string>>(perm.AllowedTransactionTypes) ?? []
        ));
    }
}