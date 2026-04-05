using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupervisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Supervisor/Index
        // Displays all pending project proposals (anonymized for supervisors)
        public async Task<IActionResult> Index()
        {
            var pendingProposals = await _context.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Pending)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(pendingProposals);
        }

        // POST: Supervisor/AcceptProposal
        // Accepts a proposal and marks it as Matched, revealing identities
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptProposal(int id)
        {
            var proposal = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            // Get the current supervisor's ID
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Update proposal status and assign supervisor
            proposal.Status = ProjectStatus.Matched;
            proposal.SupervisorId = supervisorId;
            proposal.IsIdentityRevealed = true;
            proposal.MatchedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _context.Update(proposal);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error accepting proposal: {ex.Message}");
                return RedirectToAction("Index");
            }

            // Redirect to MatchSuccess view
            return RedirectToAction("MatchSuccess", new { id = proposal.Id });
        }

        // GET: Supervisor/MatchSuccess
        // Displays matched proposal with revealed identities (only if Status is Matched)
        public async Task<IActionResult> MatchSuccess(int id)
        {
            var proposal = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            // Ensure identity reveal is only shown for matched proposals
            if (proposal.Status != ProjectStatus.Matched)
            {
                return Forbid("Identities can only be revealed for matched proposals.");
            }

            // Retrieve student and supervisor information
            var student = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == proposal.StudentId);
            var supervisor = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == proposal.SupervisorId);

            // Create view model with proposal and user details
            var viewModel = new Dictionary<string, object>
            {
                { "Proposal", proposal },
                { "Student", student },
                { "Supervisor", supervisor }
            };

            return View(viewModel);
        }
    }
}
