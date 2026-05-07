using Microsoft.AspNetCore.Mvc;

namespace FurkanTural_Admin.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
            return RedirectToAction("Login", "Auth");

        ViewData["Username"] = HttpContext.Session.GetString("username");
        return View();
    }
}
