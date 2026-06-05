using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentBridge.Controllers;


[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Index() =>
        RedirectToAction("Index", "Admin", new { area = "Admin" });
}
