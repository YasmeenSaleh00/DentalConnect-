using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentBridge.Controllers;

// This controller is intentionally minimal — all admin functionality lives in Areas/Admin.
// These redirects preserve backward compatibility for any direct links.
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Index() =>
        RedirectToAction("Index", "Admin", new { area = "Admin" });
}
