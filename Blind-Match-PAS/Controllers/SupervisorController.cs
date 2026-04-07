using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    /// <summary>
    /// Supervisor controller for browsing student project proposals and expressing interest.
    /// </summary>
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly CustomDbContext _customContext;

        public SupervisorController(CustomDbContext customContext)
        {
            _customContext = customContext;
        }

        // GET: Supervisor/Index
        // Shows pending project proposals to supervisors with blind matching.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var proposals = await _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Pending)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(proposals);
        }

        // POST: Supervisor/ExpressInterest
        // Supervisor expresses interest in a pending proposal.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpressInterest(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("Index");
            }

            if (proposal.Status != ProjectStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending proposals can be expressed interest in.";
                return RedirectToAction("Index");
            }

            proposal.SupervisorId = supervisorId;
            proposal.Status = ProjectStatus.Interested;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interest expressed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error expressing interest: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: Supervisor/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proposal = await _customContext.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (proposal == null)
            {
                return NotFound();
            }

            var currentSupervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (proposal.Status != ProjectStatus.Pending && proposal.SupervisorId != currentSupervisorId)
            {
                return Forbid();
            }

            return View(proposal);
        }

        // POST: Supervisor/ConfirmMatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMatch(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var proposal = await _customContext.ProjectProposals.FindAsync(id);
            if (proposal == null)
            {
                TempData["ErrorMessage"] = "Proposal not found.";
                return RedirectToAction("Index");
            }

            if (proposal.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Only the supervisor who expressed interest can confirm this match.";
                return RedirectToAction("Index");
            }

            if (proposal.Status != ProjectStatus.Interested)
            {
                TempData["ErrorMessage"] = "Only proposals with expressed interest can be confirmed.";
                return RedirectToAction("Index");
            }

            proposal.Status = ProjectStatus.Matched;
            proposal.IsIdentityRevealed = true;
            proposal.MatchedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.ProjectProposals.Update(proposal);
                await _customContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error confirming match: {ex.Message}";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Match confirmed successfully.";
            return RedirectToAction("Details", new { id = proposal.Id });
        }
    }
}
