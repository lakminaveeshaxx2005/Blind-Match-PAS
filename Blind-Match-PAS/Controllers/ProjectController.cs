using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using System.Security.Claims;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Project/Index
        // Shows project proposals to supervisors with blind matching for pending proposals.
        public async Task<IActionResult> Index()
        {
            var proposals = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .ToListAsync();

            var studentIds = proposals
                .Where(p => p.StudentId != null)
                .Select(p => p.StudentId)
                .Distinct()
                .ToList();

            var studentNames = await _context.ApplicationUsers
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var viewModels = proposals.Select(p => new SupervisorProjectProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea?.Name,
                Status = p.Status,
                StudentId = p.Status == ProjectStatus.Pending ? null : p.StudentId,
                StudentName = p.Status == ProjectStatus.Pending ? null : studentNames.GetValueOrDefault(p.StudentId, "Unknown"),
                IsIdentityRevealed = p.IsIdentityRevealed,
                SupervisorId = p.SupervisorId,
                MatchedAt = p.MatchedAt
            }).ToList();

            return View(viewModels);
        }

        // POST: Project/ExpressInterest
        // Matches a pending proposal and reveals student identity.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpressInterest(int id)
        {
            var proposal = await _context.ProjectProposals.FindAsync(id);

            if (proposal == null)
            {
                return NotFound("Proposal not found.");
            }

            if (proposal.Status == ProjectStatus.Matched)
            {
                return BadRequest("Proposal is already matched.");
            }

            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            proposal.Status = ProjectStatus.Matched;
            proposal.SupervisorId = supervisorId;
            proposal.IsIdentityRevealed = true;
            proposal.MatchedAt = DateTime.UtcNow;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _context.Update(proposal);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interest expressed successfully. The proposal is now matched and identity is revealed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error expressing interest: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }

    public class SupervisorProjectProposalViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string TechnicalStack { get; set; }
        public string? ResearchAreaName { get; set; }
        public ProjectStatus Status { get; set; }
        public string? StudentId { get; set; }
        public string? StudentName { get; set; }
        public bool IsIdentityRevealed { get; set; }
        public string? SupervisorId { get; set; }
        public DateTime? MatchedAt { get; set; }
    }
}
