using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentBridge.Data;
using DentBridge.Models;
using DentBridge.Models.Enums;
using DentBridge.Services.Interfaces;
using DentBridge.ViewModels.Case;
using DentBridge.ViewModels.Profile;
using DentBridge.ViewModels.Testimonial;

namespace DentBridge.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;

    public StudentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IFileService fileService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _fileService = fileService;
    }

    public async Task<IActionResult> Dashboard(TreatmentType? type = null, string? search = null)
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null)
            return View(new StudentDashboardViewModel { IsApproved = false, AccountStatus = AccountStatus.Pending });

        var myCases = await _context.DentalCases
            .Include(c => c.Images)
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.Review)
            .Where(c => c.AssignedStudentId == student.Id)
            .OrderByDescending(c => c.AcceptedAt)
            .ToListAsync();

        var available = new List<DentalCase>();
        if (student.Status == AccountStatus.Active)
        {
            var query = _context.DentalCases
                .Include(c => c.Images)
                .Include(c => c.Patient).ThenInclude(p => p.User)
                .Where(c => c.Status == CaseStatus.Open && c.IsActive);

            if (type.HasValue)
                query = query.Where(c => c.TreatmentType == type.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c =>
                    c.Title.Contains(search) ||
                    c.Description.Contains(search) ||
                    (c.Location != null && c.Location.Contains(search)));

            available = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        return View(new StudentDashboardViewModel
        {
            StudentName = student.User.FullName,
            IsApproved = student.Status == AccountStatus.Active,
            AccountStatus = student.Status,
            AvailableCases = available,
            MyCases = myCases,
            TotalAccepted = myCases.Count,
            TotalCompleted = myCases.Count(c => c.Status == CaseStatus.Completed),
            AverageRating = student.AverageRating,
            TotalRatings = student.TotalRatings,
            FilterType = type,
            SearchTerm = search
        });
    }

    public async Task<IActionResult> CaseDetails(int id)
    {
        var dentalCase = await _context.DentalCases
            .Include(c => c.Images)
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Include(c => c.StatusHistory)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase is null) return NotFound();
        return View(dentalCase);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptCase(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null || student.Status != AccountStatus.Active)
        {
            TempData["Error"] = "Your account is not yet approved.";
            return RedirectToAction(nameof(Dashboard));
        }

        var dentalCase = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase is null || dentalCase.Status != CaseStatus.Open)
        {
            TempData["Error"] = "Unable to accept this case. It may have already been accepted.";
            return RedirectToAction(nameof(Dashboard));
        }

        dentalCase.Status = CaseStatus.InProgress;
        dentalCase.AssignedStudentId = student.Id;
        dentalCase.AcceptedAt = DateTime.UtcNow;
        dentalCase.UpdatedAt = DateTime.UtcNow;

        _context.CaseStatusHistories.Add(new CaseStatusHistory
        {
            CaseId = id,
            OldStatus = CaseStatus.Open,
            NewStatus = CaseStatus.InProgress,
            ChangedByUserId = userId,
            Notes = "Case accepted by student"
        });

        var patientUser = dentalCase.Patient.User;
        _context.Notifications.Add(new Notification
        {
            UserId = patientUser.Id,
            Title = "Case Accepted!",
            Message = $"Student {student.User.FullName} accepted your case '{dentalCase.Title}'.",
            Type = NotificationType.CaseAccepted,
            ActionUrl = $"/Patient/CaseDetails/{id}"
        });

        await _context.SaveChangesAsync();
        await _emailService.SendCaseAcceptedAsync(patientUser.Email!, patientUser.FullName, dentalCase.Title, student.User.FullName);

        TempData["Success"] = "Case accepted! Please contact the patient to arrange an appointment.";
        return RedirectToAction(nameof(CaseDetails), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCase(int id, string? notes, List<IFormFile>? afterImages)
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student is null) return Forbid();

        var dentalCase = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase?.AssignedStudentId != student.Id) return Forbid();

        var oldStatus = dentalCase.Status;
        dentalCase.Status = CaseStatus.Completed;
        dentalCase.CompletedAt = DateTime.UtcNow;
        dentalCase.UpdatedAt = DateTime.UtcNow;
        student.CompletedCases++;

        _context.CaseStatusHistories.Add(new CaseStatusHistory
        {
            CaseId = id,
            OldStatus = oldStatus,
            NewStatus = CaseStatus.Completed,
            ChangedByUserId = userId,
            Notes = notes
        });

        if (afterImages is { Count: > 0 })
        {
            foreach (var file in afterImages.Where(f => _fileService.IsValidImage(f)))
            {
                var path = await _fileService.SaveCaseImageAsync(file, id);
                _context.CaseImages.Add(new CaseImage
                {
                    CaseId = id,
                    ImagePath = path,
                    IsAfterTreatment = true
                });
            }
        }

        var patientUser = dentalCase.Patient.User;
        _context.Notifications.Add(new Notification
        {
            UserId = patientUser.Id,
            Title = "Treatment Completed",
            Message = $"Your case '{dentalCase.Title}' has been completed. Please leave a review!",
            Type = NotificationType.CaseCompleted,
            ActionUrl = $"/Patient/Review/{id}"
        });

        await _context.SaveChangesAsync();
        await _emailService.SendCaseCompletedAsync(patientUser.Email!, patientUser.FullName, dentalCase.Title);

        TempData["Success"] = "Case marked as completed. The patient will be notified to leave a review.";
        return RedirectToAction(nameof(CaseDetails), new { id });
    }

    public async Task<IActionResult> MyCases()
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student is null) return Forbid();

        var cases = await _context.DentalCases
            .Include(c => c.Images)
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.Review)
            .Where(c => c.AssignedStudentId == student.Id)
            .OrderByDescending(c => c.AcceptedAt)
            .ToListAsync();

        return View(cases);
    }

    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .Include(s => s.Reviews).ThenInclude(r => r.Patient).ThenInclude(p => p.User)
            .Include(s => s.AcceptedCases)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null) return NotFound();
        return View(student);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null) return NotFound();

        return View(new EditStudentProfileViewModel
        {
            FirstName          = student.User.FirstName,
            LastName           = student.User.LastName,
            PhoneNumber        = student.User.PhoneNumber,
            PhoneNumber2       = student.User.PhoneNumber2,
            Address            = student.User.Address,
            City               = student.User.City,
            DateOfBirth        = student.User.DateOfBirth == default ? null : student.User.DateOfBirth,
            Bio                = student.Bio,
            CurrentAvatarPath  = student.User.AvatarPath,
            UniversityName     = student.UniversityName,
            StudentId          = student.StudentId,
            Specialization     = student.Specialization,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditStudentProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User)!;
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null) return NotFound();

        var user = student.User;

        if (model.Avatar is not null)
        {
            if (!_fileService.IsValidImage(model.Avatar))
            {
                ModelState.AddModelError(nameof(model.Avatar), "Please upload a valid image (JPG, PNG, WebP, max 5 MB).");
                model.CurrentAvatarPath  = user.AvatarPath;
                model.UniversityName     = student.UniversityName;
                model.StudentId          = student.StudentId;
                model.Specialization     = student.Specialization;
                return View(model);
            }

            if (!string.IsNullOrEmpty(user.AvatarPath))
                _fileService.DeleteFile(user.AvatarPath);

            user.AvatarPath = await _fileService.SaveAvatarAsync(model.Avatar, userId);
        }

        user.FirstName    = model.FirstName;
        user.LastName     = model.LastName;
        user.PhoneNumber  = model.PhoneNumber;
        user.PhoneNumber2 = model.PhoneNumber2;
        user.Address      = model.Address;
        user.City         = model.City;
        if (model.DateOfBirth.HasValue)
            user.DateOfBirth = model.DateOfBirth.Value;

        student.Bio = model.Bio;

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Notifications()
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var unread = notifications.Where(n => !n.IsRead).ToList();
        foreach (var n in unread) n.IsRead = true;
        if (unread.Count > 0) await _context.SaveChangesAsync();

        return View(notifications);
    }

    // ─── Testimonial ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> SubmitTestimonial()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        var existing = await _context.Testimonials.AnyAsync(t => t.UserId == userId);

        if (existing)
        {
            TempData["Info"] = "You have already submitted a testimonial. Thank you!";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new SubmitTestimonialViewModel
        {
            Name = user?.FullName ?? "",
            Role = "Student"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTestimonial(SubmitTestimonialViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        if (await _context.Testimonials.AnyAsync(t => t.UserId == userId))
        {
            TempData["Info"] = "You have already submitted a testimonial.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (!ModelState.IsValid) return View(model);

        _context.Testimonials.Add(new Testimonial
        {
            UserId = userId,
            Name = model.Name,
            Role = "Student",
            Rating = model.Rating,
            Message = model.Message,
            Status = DentBridge.Models.Enums.TestimonialStatus.Pending
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thank you! Your testimonial has been submitted for review.";
        return RedirectToAction(nameof(Dashboard));
    }
}
