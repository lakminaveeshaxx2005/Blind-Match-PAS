using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    // Fixed: 'Controller' must be capitalized
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Displays the Project submission page
        public async Task<IActionResult> SubmitProposal()
        {
            // Fetch Research Areas to populate the dropdown
            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View();
        }

        // 2. POST: Handles the form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Fixed: Changed 'ProjectProposal' to 'Project' to match your model
        public async Task<IActionResult> SubmitProposal(ProjectProposal model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString != null)
            {
                // Fixed: Converting String ID from Claims to Integer for the Model
                if (int.TryParse(userIdString, out int userId))
                {
                    model.StudentId = userIdString;
                    model.Status = ProjectStatus.Pending; // Matches the enum status in your ProjectProposal model

                    _context.Add(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Home");
                }
            }

            // If something fails, reload the dropdown and return the view
            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View(model);
        }
    }
}