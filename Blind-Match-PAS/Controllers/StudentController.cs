using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly CustomDbContext _context;

        public StudentController(CustomDbContext context)
        {
            _context = context;
        }

        // 1. Dashboard: Displays the student's submitted proposals
        public async Task<IActionResult> Dashboard()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposals = await _context.ProjectProposals
                .Where(p => p.StudentId == studentId)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(proposals);
        }

        // 2. GET: Displays the Proposal submission page
        public async Task<IActionResult> SubmitProposal()
        {
            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View();
        }

        // 3. POST: Handles the proposal form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitProposal(ProjectProposal model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdString != null)
            {
                model.StudentId = userIdString;
                model.Status = ProjectStatus.Pending;

                _context.ProjectProposals.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard");
            }

            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name");
            return View(model);
        }

        // 4. GET: Create Project
        public IActionResult CreateProject()
        {
            return View();
        }

        // 5. POST: Create Project
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(Project model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            model.StudentId = studentId;
            _context.Projects.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your project was submitted successfully.";
            return RedirectToAction("Dashboard");
        }
    }
}