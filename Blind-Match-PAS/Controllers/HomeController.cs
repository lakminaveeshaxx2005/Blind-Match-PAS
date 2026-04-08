using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Blind_Match_PAS.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            var userRole = User.FindFirstValue("UserRole") ?? User.FindFirst(ClaimTypes.Role)?.Value;

            return userRole switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Supervisor" => RedirectToAction("Index", "Supervisor"),
                "Student" => RedirectToAction("Index", "Matching"),
                _ => View()
            };
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
