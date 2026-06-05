using Microsoft.AspNetCore.Identity;
using DentBridge.Models;
using DentBridge.Models.Enums;

namespace DentBridge.Data;

public static class SeedData
{
    public static readonly string[] Roles = ["Admin", "Student", "Patient", "Supervisor"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@dentbridge.com";

        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            FirstName = "System",
            LastName = "Admin",
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            DateOfBirth = new DateTime(1980, 1, 1)
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

   
    }

