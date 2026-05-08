using Microsoft.AspNetCore.Identity;
using OfflinePaymentLinks.API.Models;

namespace OfflinePaymentLinks.API.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed roles
        string[] roles = ["SuperAdmin", "Admin", "Agent", "User", "CEM"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed SuperAdmin
        const string superAdminEmail = "superadmin@hffc.com";
        const string superAdminPassword = "SuperAdmin@123";

        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = "Super Admin",
                Email = superAdminEmail,
                EmailConfirmed = true,
                IsApproved = true,
                RegistrationDate = DateTime.Now,
                Approveddate = DateTime.Now,
                ApproverName = "System"
            };

            var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }
}