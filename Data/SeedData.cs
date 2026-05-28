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
        await SeedSampleDataAsync(userManager, context);
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

    private static async Task SeedSampleDataAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        // Seed a sample approved student
        const string studentEmail = "student@dentbridge.com";
        if (await userManager.FindByEmailAsync(studentEmail) is null)
        {
            var student = new ApplicationUser
            {
                FirstName = "Sara",
                LastName = "Ahmed",
                UserName = studentEmail,
                Email = studentEmail,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DateOfBirth = new DateTime(2000, 6, 15),
                PhoneNumber = "0501234567",
                City = "Riyadh"
            };

            var result = await userManager.CreateAsync(student, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student, "Student");
                context.StudentProfiles.Add(new StudentProfile
                {
                    UserId = student.Id,
                    UniversityName = "King Saud University",
                    StudentId = "KSU-2021-4521",
                    AcademicYear = 5,
                    Specialization = "General Dentistry",
                    Bio = "Passionate 5th-year dental student with hands-on clinical experience.",
                    Status = AccountStatus.Active,
                    ApprovedAt = DateTime.UtcNow,
                    ProofDocumentPath = "/uploads/proofs/sample-proof.pdf"
                });
            }
        }

        // Seed a sample patient
        const string patientEmail = "patient@dentbridge.com";
        if (await userManager.FindByEmailAsync(patientEmail) is null)
        {
            var patient = new ApplicationUser
            {
                FirstName = "Mohammed",
                LastName = "Al-Rashid",
                UserName = patientEmail,
                Email = patientEmail,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DateOfBirth = new DateTime(1990, 3, 22),
                PhoneNumber = "0559876543",
                City = "Jeddah"
            };

            var result = await userManager.CreateAsync(patient, "Patient@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(patient, "Patient");
                context.PatientProfiles.Add(new PatientProfile
                {
                    UserId = patient.Id,
                    BloodType = "O+",
                    Allergies = "None known"
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
