using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentBridge.Data;
using DentBridge.Models;
using DentBridge.Models.Enums;
using DentBridge.Services.Interfaces;
using DentBridge.ViewModels.Case;
using DentBridge.Areas.Admin.Models;

namespace DentBridge.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    // ─── Dashboard ───────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var allCases = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var pendingStudents = await _context.StudentProfiles
            .Include(s => s.User)
            .Where(s => s.Status == AccountStatus.Pending)
            .OrderByDescending(s => s.User.CreatedAt)
            .ToListAsync();

        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .ToDictionary(
                d => d.ToString("MMM dd"),
                d => allCases.Count(c => c.CreatedAt.Date == d));

        var byType = allCases
            .GroupBy(c => c.TreatmentType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return View(new AdminDashboardViewModel
        {
            TotalPatients = await _context.PatientProfiles.CountAsync(),
            TotalStudents = await _context.StudentProfiles.CountAsync(),
            PendingStudents = pendingStudents.Count,
            TotalCases = allCases.Count,
            OpenCases = allCases.Count(c => c.Status == CaseStatus.Open),
            InProgressCases = allCases.Count(c => c.Status == CaseStatus.InProgress),
            CompletedCases = allCases.Count(c => c.Status == CaseStatus.Completed),
            RecentCases = allCases.Take(8),
            PendingApprovals = pendingStudents.Take(5),
            CasesByType = byType,
            CasesLast7Days = last7Days,
            PendingTestimonials = await _context.Testimonials.CountAsync(t => t.Status == TestimonialStatus.Pending),
            ApprovedTestimonials = await _context.Testimonials.CountAsync(t => t.Status == TestimonialStatus.Approved)
        });
    }

    // ─── Students ────────────────────────────────────────────────────────────

    public async Task<IActionResult> PendingStudents()
    {
        var students = await _context.StudentProfiles
            .Include(s => s.User)
            .Where(s => s.Status == AccountStatus.Pending)
            .OrderByDescending(s => s.User.CreatedAt)
            .ToListAsync();
        return View(students);
    }

    public async Task<IActionResult> AllStudents()
    {
        var students = await _context.StudentProfiles
            .Include(s => s.User)
            .OrderByDescending(s => s.User.CreatedAt)
            .ToListAsync();
        return View(students);
    }

    public async Task<IActionResult> StudentDetails(int id)
    {
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .Include(s => s.Reviews).ThenInclude(r => r.Patient).ThenInclude(p => p.User)
            .Include(s => s.AcceptedCases)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student is null) return NotFound();
        return View(student);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveStudent(int id)
    {
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (student is null) return NotFound();

        student.Status = AccountStatus.Active;
        student.ApprovedAt = DateTime.UtcNow;
        student.ApprovedByAdminId = _userManager.GetUserId(User);
        student.RejectionReason = null;

        _context.Notifications.Add(new Notification
        {
            UserId = student.UserId,
            Title = "Account Approved!",
            Message = "Your DentBridge student account has been approved. You can now accept cases.",
            Type = NotificationType.AccountApproved,
            ActionUrl = "/Student/Dashboard"
        });

        await _context.SaveChangesAsync();
        await _emailService.SendAccountApprovedAsync(student.User.Email!, student.User.FullName);

        TempData["Success"] = $"{student.User.FullName}'s account has been approved successfully.";
        return RedirectToAction(nameof(PendingStudents));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectStudent(int id, string reason)
    {
        var student = await _context.StudentProfiles
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (student is null) return NotFound();

        student.Status = AccountStatus.Rejected;
        student.RejectionReason = reason;

        _context.Notifications.Add(new Notification
        {
            UserId = student.UserId,
            Title = "Account Application Update",
            Message = $"Your application was not approved. Reason: {reason}",
            Type = NotificationType.AccountRejected
        });

        await _context.SaveChangesAsync();
        await _emailService.SendAccountRejectedAsync(student.User.Email!, student.User.FullName, reason);

        TempData["Warning"] = "Student application has been rejected.";
        return RedirectToAction(nameof(PendingStudents));
    }

    // ─── Cases ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> AllCases(CaseStatus? status = null)
    {
        var query = _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent).ThenInclude(s => s!.User)
            .Where(c => c.IsActive);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        ViewBag.SelectedStatus = status;
        return View(await query.OrderByDescending(c => c.CreatedAt).ToListAsync());
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
    public async Task<IActionResult> UpdateCaseStatus(int id, CaseStatus status, string? notes)
    {
        var dentalCase = await _context.DentalCases
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.AssignedStudent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dentalCase is null) return NotFound();

        var oldStatus = dentalCase.Status;
        dentalCase.Status = status;
        dentalCase.UpdatedAt = DateTime.UtcNow;

        if (status == CaseStatus.Completed)
        {
            dentalCase.CompletedAt = DateTime.UtcNow;
            if (dentalCase.AssignedStudent is not null)
                dentalCase.AssignedStudent.CompletedCases++;
        }

        _context.CaseStatusHistories.Add(new CaseStatusHistory
        {
            CaseId = id,
            OldStatus = oldStatus,
            NewStatus = status,
            ChangedByUserId = _userManager.GetUserId(User)!,
            Notes = notes
        });

        var patientUser = dentalCase.Patient.User;
        if (status == CaseStatus.Completed)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = patientUser.Id,
                Title = "Treatment Completed",
                Message = $"Your case '{dentalCase.Title}' has been completed. Please leave a review!",
                Type = NotificationType.CaseCompleted,
                ActionUrl = $"/Patient/Review/{id}"
            });
        }

        await _context.SaveChangesAsync();

        if (status == CaseStatus.Completed)
            await _emailService.SendCaseCompletedAsync(patientUser.Email!, patientUser.FullName, dentalCase.Title);

        TempData["Success"] = $"Case status updated to {status}.";
        return RedirectToAction(nameof(CaseDetails), new { id });
    }

    // ─── Notifications ───────────────────────────────────────────────────────

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

    // ─── Users ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> AllUsers()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            userRoles[u.Id] = await _userManager.GetRolesAsync(u);

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        if (!user.IsActive)
            await _userManager.UpdateSecurityStampAsync(user);
        else
            await _userManager.UpdateAsync(user);

        TempData["Success"] = user.IsActive ? "User activated successfully." : "User deactivated successfully.";
        return RedirectToAction(nameof(AllUsers));
    }

    // ─── Testimonials ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Testimonials(TestimonialStatus? filter = null)
    {
        var query = _context.Testimonials
            .Include(t => t.User)
            .AsQueryable();

        if (filter.HasValue)
            query = query.Where(t => t.Status == filter.Value);

        var all = await _context.Testimonials.Include(t => t.User).OrderByDescending(t => t.CreatedAt).ToListAsync();

        var vm = new AdminTestimonialsViewModel
        {
            Pending = all.Where(t => t.Status == TestimonialStatus.Pending),
            Approved = all.Where(t => t.Status == TestimonialStatus.Approved),
            Rejected = all.Where(t => t.Status == TestimonialStatus.Rejected),
            TotalCount = all.Count,
            Filter = filter,
            Filtered = filter.HasValue
                ? all.Where(t => t.Status == filter.Value)
                : all
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveTestimonial(int id)
    {
        var testimonial = await _context.Testimonials
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (testimonial is null) return NotFound();

        testimonial.Status = TestimonialStatus.Approved;
        testimonial.ReviewedAt = DateTime.UtcNow;
        testimonial.ReviewedByAdminId = _userManager.GetUserId(User);

        _context.Notifications.Add(new Notification
        {
            UserId = testimonial.UserId,
            Title = "Testimonial Approved!",
            Message = "Your testimonial has been approved and is now visible on the home page.",
            Type = NotificationType.TestimonialApproved,
            ActionUrl = "/"
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Testimonial approved and published.";
        return RedirectToAction(nameof(Testimonials));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectTestimonial(int id, string? notes)
    {
        var testimonial = await _context.Testimonials
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (testimonial is null) return NotFound();

        testimonial.Status = TestimonialStatus.Rejected;
        testimonial.ReviewedAt = DateTime.UtcNow;
        testimonial.ReviewedByAdminId = _userManager.GetUserId(User);
        testimonial.AdminNotes = notes;

        _context.Notifications.Add(new Notification
        {
            UserId = testimonial.UserId,
            Title = "Testimonial Not Approved",
            Message = "Your testimonial was reviewed but could not be published at this time.",
            Type = NotificationType.TestimonialRejected
        });

        await _context.SaveChangesAsync();

        TempData["Warning"] = "Testimonial rejected.";
        return RedirectToAction(nameof(Testimonials));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTestimonial(int id)
    {
        var testimonial = await _context.Testimonials.FindAsync(id);
        if (testimonial is null) return NotFound();

        _context.Testimonials.Remove(testimonial);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Testimonial deleted.";
        return RedirectToAction(nameof(Testimonials));
    }
}
