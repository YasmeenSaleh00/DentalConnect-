using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DentBridge.Data;
using DentBridge.Models;
using DentBridge.Models.Enums;
using DentBridge.Services.Interfaces;
using DentBridge.ViewModels.Account;

namespace DentBridge.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IEmailService _emailService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IFileService fileService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _fileService = fileService;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Account locked. Please try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterPatient() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new RegisterViewModel { Role = "Patient" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPatient(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth,
            City = model.City,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Patient");
        _context.PatientProfiles.Add(new PatientProfile { UserId = user.Id });
        await _context.SaveChangesAsync();

        await _emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Welcome to DentBridge! Your account has been created.";
        return RedirectToAction("Dashboard", "Patient");
    }

    [HttpGet]
    public IActionResult RegisterStudent() =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new StudentRegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStudent(StudentRegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!_fileService.IsValidDocument(model.ProofDocument))
        {
            ModelState.AddModelError("ProofDocument", "Please upload a valid PDF or image (max 10 MB).");
            return View(model);
        }

        var user = new ApplicationUser
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth,
            City = model.City,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Student");
        var proofPath = await _fileService.SaveProofDocumentAsync(model.ProofDocument, user.Id);

        _context.StudentProfiles.Add(new StudentProfile
        {
            UserId = user.Id,
            UniversityName = model.UniversityName,
            StudentId = model.StudentId,
            AcademicYear = 5,
            Specialization = model.Specialization,
            Bio = model.Bio,
            ProofDocumentPath = proofPath,
            Status = AccountStatus.Pending
        });

        await _context.SaveChangesAsync();
        TempData["Info"] = "Your application has been submitted. You will be notified once approved.";
        return RedirectToAction("PendingApproval");
    }

    [HttpGet]
    public IActionResult PendingApproval() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
