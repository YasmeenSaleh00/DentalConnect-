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

[Authorize(Roles = "Patient")]
public class PatientController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;

    public PatientController(
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

    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User)!;

        var cases = await _context.DentalCases
            .Include(c => c.Images)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Include(c => c.Review)
            .Where(c => c.Patient.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);
        return View(new CaseDashboardViewModel
        {
            MyCases = cases,
            TotalCases = cases.Count,
            OpenCases = cases.Count(c => c.Status == CaseStatus.Open),
            InProgressCases = cases.Count(c => c.Status == CaseStatus.InProgress),
            CompletedCases = cases.Count(c => c.Status == CaseStatus.Completed),
            PatientName = user?.FullName ?? ""
        });
    }

    [HttpGet]
    public IActionResult CreateCase() => View(new CreateCaseViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCase(CreateCaseViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User)!;
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient is null) return Forbid();

        var dentalCase = new DentalCase
        {
            Title = model.Title,
            Description = model.Description,
            TreatmentType = model.TreatmentType,
            UrgencyLevel = model.UrgencyLevel,
            Location = model.Location,
            PatientNotes = model.PatientNotes,
            AppointmentDate = model.AppointmentDate,
            PatientProfileId = patient.Id
        };

        _context.DentalCases.Add(dentalCase);
        await _context.SaveChangesAsync();

        if (model.Images is { Count: > 0 })
        {
            bool first = true;
            foreach (var img in model.Images.Where(f => _fileService.IsValidImage(f)))
            {
                var path = await _fileService.SaveCaseImageAsync(img, dentalCase.Id);
                _context.CaseImages.Add(new CaseImage { CaseId = dentalCase.Id, ImagePath = path, IsPrimary = first });
                first = false;
            }
            await _context.SaveChangesAsync();
        }

        // Notify admins about new case
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = admin.Id,
                Title = "New Dental Case Posted",
                Message = $"A new case '{dentalCase.Title}' was posted.",
                Type = NotificationType.NewCase,
                ActionUrl = $"/Admin/CaseDetails/{dentalCase.Id}"
            });
        }
        if (admins.Count > 0) await _context.SaveChangesAsync();

        TempData["Success"] = "Your dental case has been posted successfully!";
        return RedirectToAction(nameof(CaseDetails), new { id = dentalCase.Id });
    }

    public async Task<IActionResult> CaseDetails(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var dentalCase = await _context.DentalCases
            .Include(c => c.Images)
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Include(c => c.StatusHistory)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase is null || dentalCase.Patient.UserId != userId) return Forbid();
        return View(dentalCase);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var dentalCase = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase is null || dentalCase.Patient.UserId != userId) return Forbid();
        if (dentalCase.Status != CaseStatus.Completed) return BadRequest();
        if (dentalCase.Review is not null) return RedirectToAction(nameof(CaseDetails), new { id });

        return View(new SubmitReviewViewModel
        {
            CaseId = id,
            CaseTitle = dentalCase.Title,
            StudentName = dentalCase.AssignedStudent?.User.FullName ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(SubmitReviewViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User)!;
        var dentalCase = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == model.CaseId);

        if (dentalCase is null || dentalCase.Patient.UserId != userId
            || dentalCase.Status != CaseStatus.Completed
            || dentalCase.Review is not null
            || dentalCase.AssignedStudentId is null)
        {
            ModelState.AddModelError("", "Unable to submit review. Please try again.");
            return View(model);
        }

        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient is null)
        {
            ModelState.AddModelError("", "Unable to submit review. Please try again.");
            return View(model);
        }

        _context.Reviews.Add(new Review
        {
            CaseId = model.CaseId,
            PatientProfileId = patient.Id,
            StudentProfileId = dentalCase.AssignedStudentId.Value,
            Rating = model.Rating,
            Comment = model.Comment,
            IsProfessional = model.IsProfessional,
            IsTimely = model.IsTimely,
            IsRecommended = model.IsRecommended
        });

        // Recalculate student average rating
        var student = dentalCase.AssignedStudent!;
        var existingRatings = await _context.Reviews
            .Where(r => r.StudentProfileId == student.Id)
            .Select(r => r.Rating)
            .ToListAsync();
        student.TotalRatings++;
        student.AverageRating = (existingRatings.Sum() + model.Rating) / (double)(existingRatings.Count + 1);

        _context.Notifications.Add(new Notification
        {
            UserId = student.UserId,
            Title = "New Review Received",
            Message = $"You received a {model.Rating}★ review for case '{dentalCase.Title}'.",
            Type = NotificationType.ReviewReceived
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Thank you for your review!";
        return RedirectToAction(nameof(CaseDetails), new { id = model.CaseId });
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


    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User)!;
        var patient = await _context.PatientProfiles
            .Include(p => p.User)
            .Include(p => p.Cases)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient is null) return NotFound();
        return View(patient);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = _userManager.GetUserId(User)!;
        var patient = await _context.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient is null) return NotFound();

        return View(new EditPatientProfileViewModel
        {
            FirstName              = patient.User.FirstName,
            LastName               = patient.User.LastName,
            PhoneNumber            = patient.User.PhoneNumber,
            PhoneNumber2           = patient.User.PhoneNumber2,
            Address                = patient.User.Address,
            City                   = patient.User.City,
            DateOfBirth            = patient.User.DateOfBirth == default ? null : patient.User.DateOfBirth,
            MedicalHistory         = patient.MedicalHistory,
            Allergies              = patient.Allergies,
            EmergencyContactName   = patient.EmergencyContactName,
            EmergencyContactPhone  = patient.EmergencyContactPhone,
            InsuranceProvider      = patient.InsuranceProvider,
            BloodType              = patient.BloodType,
            CurrentAvatarPath      = patient.User.AvatarPath,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditPatientProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User)!;
        var patient = await _context.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient is null) return NotFound();

        var user = patient.User;

        if (model.Avatar is not null)
        {
            if (!_fileService.IsValidImage(model.Avatar))
            {
                ModelState.AddModelError(nameof(model.Avatar), "Please upload a valid image (JPG, PNG, WebP, max 5 MB).");
                model.CurrentAvatarPath = user.AvatarPath;
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

        patient.MedicalHistory        = model.MedicalHistory;
        patient.Allergies             = model.Allergies;
        patient.EmergencyContactName  = model.EmergencyContactName;
        patient.EmergencyContactPhone = model.EmergencyContactPhone;
        patient.InsuranceProvider     = model.InsuranceProvider;
        patient.BloodType             = model.BloodType;

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }


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
            Role = "Patient"
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
            Role = "Patient",
            Rating = model.Rating,
            Message = model.Message,
            Status = DentBridge.Models.Enums.TestimonialStatus.Pending
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thank you! Your testimonial has been submitted for review.";
        return RedirectToAction(nameof(Dashboard));
    }
}
