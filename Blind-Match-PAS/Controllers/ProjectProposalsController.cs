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
    public class ProjectProposalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectProposalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProjectProposals/Index - Student's proposal list
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var studentProposals = await _context.ProjectProposals
                .Where(p => p.StudentId == userId)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(studentProposals);
        }

        // GET: ProjectProposals/Create
        public async Task<IActionResult> Create()
        {
            // Fetch Research Areas to populate the dropdown
            ViewBag.ResearchAreas = new SelectList(
                await _context.ResearchAreas.ToListAsync(),
                "Id",
                "Name"
            );
            return View();
        }

        // POST: ProjectProposals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectProposal model)
        {
            // Get the current logged-in user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                ModelState.AddModelError("", "User must be logged in to submit a proposal.");
                ViewBag.ResearchAreas = new SelectList(
                    await _context.ResearchAreas.ToListAsync(),
                    "Id",
                    "Name"
                );
                return View(model);
            }

            // Set StudentId and Status
            model.StudentId = userId;
            model.Status = ProjectStatus.Pending;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project proposal submitted successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving proposal: {ex.Message}");
                }
            }

            // Reload the dropdown if validation fails
            ViewBag.ResearchAreas = new SelectList(
                await _context.ResearchAreas.ToListAsync(),
                "Id",
                "Name",
                model.ResearchAreaId
            );
            return View(model);
        }

        // GET: ProjectProposals/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var proposal = await _context.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            // Authorization: Only the student who created it can edit
            if (proposal.StudentId != userId)
            {
                return Forbid("You can only edit your own proposals.");
            }

            // Only allow editing if status is Pending
            if (proposal.Status != ProjectStatus.Pending)
            {
                return BadRequest("You can only edit proposals with 'Pending' status.");
            }

            ViewBag.ResearchAreas = new SelectList(
                await _context.ResearchAreas.ToListAsync(),
                "Id",
                "Name",
                proposal.ResearchAreaId
            );

            return View(proposal);
        }

        // POST: ProjectProposals/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectProposal model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var proposal = await _context.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            if (proposal.StudentId != userId)
            {
                return Forbid("You can only edit your own proposals.");
            }

            if (proposal.Status != ProjectStatus.Pending)
            {
                return BadRequest("You can only edit proposals with 'Pending' status.");
            }

            // Update only editable fields
            proposal.Title = model.Title;
            proposal.Abstract = model.Abstract;
            proposal.TechnicalStack = model.TechnicalStack;
            proposal.ResearchAreaId = model.ResearchAreaId;
            proposal.LastModifiedAt = DateTime.UtcNow;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proposal);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project proposal updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating proposal: {ex.Message}");
                }
            }

            ViewBag.ResearchAreas = new SelectList(
                await _context.ResearchAreas.ToListAsync(),
                "Id",
                "Name",
                proposal.ResearchAreaId
            );

            return View(proposal);
        }

        // POST: ProjectProposals/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var proposal = await _context.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            if (proposal.StudentId != userId)
            {
                return Forbid("You can only delete your own proposals.");
            }

            // Only allow deletion if status is Pending
            if (proposal.Status != ProjectStatus.Pending)
            {
                return BadRequest("You can only delete proposals with 'Pending' status.");
            }

            try
            {
                _context.ProjectProposals.Remove(proposal);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project proposal deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting proposal: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
