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
        private readonly CustomDbContext _customContext;
        private readonly ApplicationDbContext _identityContext;

        public ProjectProposalsController(CustomDbContext customContext, ApplicationDbContext identityContext)
        {
            _customContext = customContext;
            _identityContext = identityContext;
        }

        // GET: ProjectProposals/Index - Student's proposal list
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var studentProposals = await _customContext.ProjectProposals
                .Where(p => p.StudentId == userId)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            var supervisorIds = studentProposals
                .Where(p => !string.IsNullOrEmpty(p.SupervisorId))
                .Select(p => p.SupervisorId!)
                .Distinct()
                .ToList();

            var supervisors = await _identityContext.ApplicationUsers
                .Where(u => supervisorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var viewModels = studentProposals.Select(p => new StudentProjectProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea?.Name,
                Status = p.Status,
                SupervisorId = p.SupervisorId,
                SupervisorName = p.SupervisorId != null ? supervisors.GetValueOrDefault(p.SupervisorId)?.FullName : null,
                SupervisorEmail = p.SupervisorId != null ? supervisors.GetValueOrDefault(p.SupervisorId)?.Email : null,
                CreatedAt = p.CreatedAt,
                MatchedAt = p.MatchedAt,
                LastModifiedAt = p.LastModifiedAt
            }).ToList();

            return View(viewModels);
        }

        // GET: ProjectProposals/Create
        public async Task<IActionResult> Create()
        {
            // Fetch Research Areas to populate the dropdown
            ViewBag.ResearchAreas = new SelectList(
                await _customContext.ResearchAreas.ToListAsync(),
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
                    await _customContext.ResearchAreas.ToListAsync(),
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
                    _customContext.Add(model);
                    await _customContext.SaveChangesAsync();
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
                await _customContext.ResearchAreas.ToListAsync(),
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
            var proposal = await _customContext.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            // Authorization: Only the student who created it can edit
            if (proposal.StudentId != userId)
            {
                return Forbid("You can only edit your own proposals.");
            }

            // Only allow editing if proposal.CanEdit (status is Pending)
            if (!proposal.CanEdit)
            {
                return BadRequest("You can only edit proposals with 'Pending' status.");
            }

            ViewBag.ResearchAreas = new SelectList(
                await _customContext.ResearchAreas.ToListAsync(),
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
            var proposal = await _customContext.ProjectProposals.FindAsync(id);

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

            // Use CanEdit property for validation
            if (!proposal.CanEdit)
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
                    _customContext.Update(proposal);
                    await _customContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project proposal updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating proposal: {ex.Message}");
                }
            }

            ViewBag.ResearchAreas = new SelectList(
                await _customContext.ResearchAreas.ToListAsync(),
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
            var proposal = await _customContext.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            if (proposal.StudentId != userId)
            {
                return Forbid("You can only delete your own proposals.");
            }

            // Only allow deletion if proposal.CanEdit (status is Pending)
            if (!proposal.CanEdit)
            {
                return BadRequest("You can only delete proposals with 'Pending' status.");
            }

            try
            {
                _customContext.ProjectProposals.Remove(proposal);
                await _customContext.SaveChangesAsync();
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
