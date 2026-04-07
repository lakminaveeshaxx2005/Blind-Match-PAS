using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    /// <summary>
    /// Supervisor controller for browsing and matching with student proposals.
    /// Implements blind matching workflow with proper state transitions.
    /// 
    /// Workflow:
    /// 1. Supervisor sees Pending proposals (student identity hidden)
    /// 2. Supervisor expresses interest → Status: UnderReview (waiting for student confirmation)
    /// 3. Student confirms → Status: Matched (both identities revealed)
    /// 4. Student can reject → Status: Pending (supervisor interest withdrawn)
    /// </summary>
    [Authorize(Roles = "Supervisor")]
    public class ProjectController : Controller
    {
        private readonly CustomDbContext _customContext;
        private readonly ApplicationDbContext _identityContext;

        public ProjectController(CustomDbContext customContext, ApplicationDbContext identityContext)
        {
            _customContext = customContext;
            _identityContext = identityContext;
        }

        // GET: Project/Index
        // Shows pending project proposals to supervisors with blind matching.
        // Student identity is hidden for all pending proposals.
        public async Task<IActionResult> Index()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var supervisor = await _identityContext.ApplicationUsers.FindAsync(supervisorId);
            var preferredAreas = ParseResearchAreaPreferences(supervisor?.Expertise);

            // Query only PENDING proposals for blind review
            var proposalsQuery = _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Pending)
                .Include(p => p.ResearchArea)
                .AsQueryable();

            // Filter by supervisor's preferred research areas if set
            if (preferredAreas.Any())
            {
                var normalizedAreas = preferredAreas.Select(a => a.ToLowerInvariant()).ToList();
                proposalsQuery = proposalsQuery.Where(p =>
                    p.ResearchArea != null &&
                    normalizedAreas.Contains(p.ResearchArea.Name!.ToLower()));
            }

            var proposals = await proposalsQuery.ToListAsync();

            // Build view models with identity masking
            var viewModels = proposals.Select(p => new SupervisorProjectProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea?.Name,
                Status = p.Status,
                StudentId = null,  // Always hidden for blind matching
                StudentName = null,  // Always hidden for blind matching
                IsIdentityRevealed = false,  // Always false during blind review
                SupervisorId = null,
                SupervisorName = null,
                SupervisorEmail = null,
                MatchedAt = null
            }).OrderByDescending(p => p.MatchedAt).ToList();

            return View(viewModels);
        }

        // GET: Project/MyMatches
        // Shows proposals the supervisor has expressed interest in or matched with.
        [HttpGet]
        public async Task<IActionResult> MyMatches()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var supervisor = await _identityContext.ApplicationUsers.FindAsync(supervisorId);
            var proposals = await _customContext.ProjectProposals
                .Where(p => p.SupervisorId == supervisorId && (p.Status == ProjectStatus.UnderReview || p.Status == ProjectStatus.Matched))
                .Include(p => p.ResearchArea)
                .ToListAsync();

            var studentIds = proposals
                .Where(p => !string.IsNullOrEmpty(p.StudentId))
                .Select(p => p.StudentId!)
                .Distinct()
                .ToList();

            var studentInfo = await _identityContext.ApplicationUsers
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.Email });

            var viewModels = proposals.Select(p => new SupervisorProjectProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea?.Name,
                Status = p.Status,
                StudentId = p.StudentId,
                StudentName = p.Status == ProjectStatus.Matched ? studentInfo.GetValueOrDefault(p.StudentId)?.FullName : null,
                IsIdentityRevealed = p.IsIdentityRevealed,
                SupervisorId = supervisorId,
                SupervisorName = supervisor?.FullName,
                SupervisorEmail = supervisor?.Email,
                MatchedAt = p.MatchedAt
            }).OrderByDescending(p => p.MatchedAt).ToList();

            return View(viewModels);
        }

        // GET: Project/Preferences
        // View and edit research area preferences.
        [HttpGet]
        public async Task<IActionResult> Preferences()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var supervisor = await _identityContext.ApplicationUsers.FindAsync(supervisorId);
            if (supervisor == null)
            {
                return NotFound("Supervisor not found.");
            }

            var model = new SupervisorPreferencesViewModel
            {
                Expertise = supervisor.Expertise
            };

            return View(model);
        }

        // POST: Project/Preferences
        // Update research area preferences.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preferences(SupervisorPreferencesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(supervisorId))
            {
                return Unauthorized("Supervisor ID not found.");
            }

            var supervisor = await _identityContext.ApplicationUsers.FindAsync(supervisorId);
            if (supervisor == null)
            {
                return NotFound("Supervisor not found.");
            }

            supervisor.Expertise = model.Expertise?.Trim();
            _identityContext.Update(supervisor);
            await _identityContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your preferred research areas were updated successfully.";
            return RedirectToAction("Index");
        }

        // POST: Project/ExpressInterest
        // Supervisor expresses interest in a pending proposal.
        // Transitions proposal from Pending → UnderReview.
        // Student identity remains hidden. Awaits student confirmation.
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

            // Validate state transition using model method
            if (!proposal.IsValidStateTransition(ProjectStatus.UnderReview))
            {
                TempData["ErrorMessage"] = $"Cannot express interest in a proposal with status '{proposal.Status}'. Only pending proposals can receive interest.";
                return RedirectToAction("Index");
            }

            // Transition to UnderReview - identities still hidden
            proposal.Status = ProjectStatus.UnderReview;
            proposal.SupervisorId = supervisorId;
            proposal.IsIdentityRevealed = false;  // KEY: Identity NOT revealed yet
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _identityContext.Update(proposal);
                await _identityContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interest expressed successfully. Waiting for student confirmation.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error expressing interest: {ex.Message}";
            }

            return RedirectToAction("MyMatches");
        }

        // POST: Project/WithdrawInterest
        // Supervisor withdraws interest in an UnderReview proposal.
        // Transitions back from UnderReview → Pending.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawInterest(int id)
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
                return RedirectToAction("MyMatches");
            }

            // Verify proposal is assigned to this supervisor
            if (proposal.SupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "You cannot withdraw interest from this proposal.";
                return RedirectToAction("MyMatches");
            }

            // Validate state transition using model method
            if (!proposal.IsValidStateTransition(ProjectStatus.Pending))
            {
                TempData["ErrorMessage"] = $"Cannot withdraw interest from a proposal with status '{proposal.Status}'. Only under-review proposals can have interest withdrawn.";
                return RedirectToAction("MyMatches");
            }

            // Transition back to Pending
            proposal.Status = ProjectStatus.Pending;
            proposal.SupervisorId = null;
            proposal.IsIdentityRevealed = false;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _identityContext.Update(proposal);
                await _identityContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interest withdrawn successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error withdrawing interest: {ex.Message}";
            }

            return RedirectToAction("MyMatches");
        }

        private static List<string> ParseResearchAreaPreferences(string? expertise)
        {
            if (string.IsNullOrWhiteSpace(expertise))
            {
                return new List<string>();
            }

            return expertise
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(area => area.Trim())
                .Where(area => !string.IsNullOrEmpty(area))
                .ToList();
        }
    }
}

