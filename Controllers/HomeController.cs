using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentBridge.Data;
using DentBridge.Models;
using DentBridge.Models.Enums;
using DentBridge.Services.Interfaces;
using DentBridge.ViewModels;

namespace DentBridge.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public HomeController(
        ILogger<HomeController> logger,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        if (_signInManager.IsSignedIn(User))
        {
            var user = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(user!, "Admin"))
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            if (await _userManager.IsInRoleAsync(user!, "Student"))
                return RedirectToAction("Dashboard", "Student");
            if (await _userManager.IsInRoleAsync(user!, "Patient"))
                return RedirectToAction("Dashboard", "Patient");
        }

        var testimonials = await _context.Testimonials
            .Where(t => t.Status == TestimonialStatus.Approved)
            .OrderByDescending(t => t.ReviewedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Testimonials = testimonials;
        return View();
    }

    public IActionResult About() => View();
    public IActionResult HowItWorks() => View();
    public IActionResult Privacy() => View();

    public IActionResult Contact() => View(new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _emailService.SendContactMessageAsync(model.Name, model.Email, model.Subject, model.Message);
        TempData["Success"] = "Your message has been sent! We'll get back to you soon.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
