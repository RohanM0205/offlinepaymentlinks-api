using System.Security.Claims;
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
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // ── Pending approvals ─────────────────────────────────────────────────

    [HttpGet("access-requests")]
    public async Task<IActionResult> GetAccessRequests([FromQuery] string? searchEmail)
    {
        var query = _userManager.Users.Where(u => !u.IsApproved);

        if (!string.IsNullOrWhiteSpace(searchEmail))
            query = query.Where(u => EF.Functions.Like(u.Email!, $"%{searchEmail.Trim()}%"));

        var pendingUsers = await query
            .OrderByDescending(u => u.RegistrationDate)
            .Select(u => new {
                u.Id,
                u.Email,
                u.UserName,
                u.RegistrationDate,
                u.IsApproved
            })
            .ToListAsync();

        var isSuperAdmin = User.IsInRole("SuperAdmin");

        var availableRoles = await _roleManager.Roles
            .Where(r => isSuperAdmin
                ? r.Name != "SuperAdmin"
                : r.Name != "SuperAdmin" && r.Name != "Admin")
            .Select(r => r.Name)
            .ToListAsync();

        return Ok(new { pendingUsers, availableRoles });
    }

    [HttpPost("approve-user")]
    public async Task<IActionResult> ApproveUser([FromBody] ApproveUserRequest req)
    {
        if (string.IsNullOrEmpty(req.RoleName))
            return BadRequest(new { message = "Please select a role before approving." });

        var user = await _userManager.FindByIdAsync(req.UserId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var approverEmail = User.FindFirstValue(ClaimTypes.Email);

        user.IsApproved = true;
        user.Approveddate = DateTime.Now;
        user.ApproverName = approverEmail ?? "System";

        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, req.RoleName);

        return Ok(new { message = $"User approved and assigned role: {req.RoleName}" });
    }

    [HttpPost("reject-user")]
    public async Task<IActionResult> RejectUser([FromBody] RejectUserRequest req)
    {
        var user = await _userManager.FindByIdAsync(req.UserId);
        if (user == null) return NotFound(new { message = "User not found." });
        await _userManager.DeleteAsync(user);
        return Ok(new { message = "User rejected and removed." });
    }

    // ── View users (non-admin) ────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] string? searchEmail = null)
    {
        var query = from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where !new[] { "Admin", "SuperAdmin" }.Contains(role.Name) && user.IsApproved
                    select new
                    {
                        user.Id,
                        user.Email,
                        user.IsApproved,
                        user.ApproverName,
                        user.RegistrationDate,
                        ApprovedDate = user.Approveddate,
                        RoleName = role.Name
                    };

        if (!string.IsNullOrWhiteSpace(searchEmail))
            query = query.Where(u => u.Email!.Contains(searchEmail));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.ApprovedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Ok(new { items, total, page, pageSize = PageSize });
    }

    [HttpGet("users/all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] string? searchEmail = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isApproved = null)
    {
        var query = from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join r in _context.Roles on userRole.RoleId equals r.Id
                    where r.Name != "Admin" && r.Name != "SuperAdmin"
                    select new
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.IsApproved,
                        user.ApproverName,
                        user.RegistrationDate,
                        ApprovedDate = user.Approveddate,
                        RoleName = r.Name
                    };

        if (!string.IsNullOrWhiteSpace(searchEmail))
            query = query.Where(u => u.Email!.Contains(searchEmail));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.RoleName == role);

        if (isApproved.HasValue)
            query = query.Where(u => u.IsApproved == isApproved.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.RegistrationDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var roles = await _roleManager.Roles
            .Where(r => r.Name != "Admin" && r.Name != "SuperAdmin")
            .Select(r => r.Name)
            .ToListAsync();

        return Ok(new { items, total, page, pageSize = PageSize, roles });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });
        await _userManager.DeleteAsync(user);
        return Ok(new { message = "User deleted." });
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateUserRoleRequest req)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        if (req.NewRole == "Admin" || req.NewRole == "SuperAdmin")
            return BadRequest(new { message = "Cannot assign Admin or SuperAdmin via this endpoint." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, req.NewRole);

        return Ok(new { message = $"Role updated to {req.NewRole}" });
    }

    // ── Roles ─────────────────────────────────────────────────────────────

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return Ok(roles);
    }

    [HttpGet("roles/assignable")]
    public async Task<IActionResult> GetAssignableRoles()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var roles = await _roleManager.Roles
            .Where(r => isSuperAdmin
                ? r.Name != "SuperAdmin"
                : r.Name != "SuperAdmin" && r.Name != "Admin")
            .Select(r => r.Name)
            .ToListAsync();
        return Ok(roles);
    }

    [HttpPost("roles")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateRole([FromBody] string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return Conflict(new { message = "Role already exists." });

        await _roleManager.CreateAsync(new IdentityRole(roleName));
        return Ok(new { message = $"Role '{roleName}' created." });
    }

    [HttpDelete("roles/{roleName}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound(new { message = "Role not found." });
        await _roleManager.DeleteAsync(role);
        return Ok(new { message = $"Role '{roleName}' deleted." });
    }

    // ── Admins (SuperAdmin only) ──────────────────────────────────────────

    [HttpGet("admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAdmins(
        [FromQuery] int page = 1,
        [FromQuery] string? searchEmail = null)
    {
        var query = from user in _context.Users
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where role.Name == "Admin"
                    select new
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.IsApproved,
                        user.ApproverName,
                        user.RegistrationDate,
                        ApprovedDate = user.Approveddate,
                        RoleName = role.Name
                    };

        if (!string.IsNullOrWhiteSpace(searchEmail))
            query = query.Where(u => u.Email!.Contains(searchEmail));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.ApprovedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Ok(new { items, total, page, pageSize = PageSize });
    }

    [HttpPost("admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest req)
    {
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var approverEmail = User.FindFirstValue(ClaimTypes.Email);

        var user = new ApplicationUser
        {
            UserName = req.Name,
            Email = req.Email,
            EmailConfirmed = true,
            IsApproved = true,
            RegistrationDate = DateTime.Now,
            Approveddate = DateTime.Now,
            ApproverName = approverEmail ?? "System"
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, "Admin");
        return Ok(new { message = "Admin created successfully." });
    }

    [HttpDelete("admins/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteAdmin(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "Admin not found." });
        await _userManager.DeleteAsync(user);
        return Ok(new { message = "Admin deleted." });
    }

    [HttpPut("admins/{userId}/role")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateAdminRole(string userId, [FromBody] UpdateUserRoleRequest req)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == requesterId)
            return BadRequest(new { message = "You cannot change your own role." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, req.NewRole);

        return Ok(new { message = $"Role updated to {req.NewRole}" });
    }
}