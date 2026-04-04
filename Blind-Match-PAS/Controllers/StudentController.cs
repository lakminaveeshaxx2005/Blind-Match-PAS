using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor to inject the database context
        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Displays the Project Proposal submission page
        public async Task<IActionResult> SubmitProposal()
        {
            // Fetch Research Areas from the database to populate the dropdown list
            ViewBag.ResearchAreas = new SelectList(await _context.Set<ResearchArea>().ToListAsync(), "Id", "Name");
            return View();
        }

        // 2. POST: Handles the form submission and saves data to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitProposal(ProjectProposal model)
        {
            // Retrieve the Unique ID of the currently logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                // Assign the logged-in user's ID to the proposal
                model.StudentId = userId;

                // Set initial status to '0' (Pending) for new submissions
                model.Status = 0;

                _context.Add(model);
                await _context.SaveChangesAsync();

                // Redirect to Home Page after successful submission
                return RedirectToAction("Index", "Home");
            }

            // If user is not found or submission fails, reload the dropdown and return the view
            ViewBag.ResearchAreas = new SelectList(await _context.Set<ResearchArea>().ToListAsync(), "Id", "Name");
            return View(model);
        }
    }
}