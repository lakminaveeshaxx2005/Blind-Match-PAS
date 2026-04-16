using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Blind_Match_PAS.Services;
using Blind_Match_PAS.Repositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly CustomDbContext _context;
        private readonly ApplicationDbContext _applicationContext;
        private readonly IMatchingService _matchingService;
        private readonly IMatchingRequestRepository _matchingRequestRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(CustomDbContext context, ApplicationDbContext applicationContext, IMatchingService matchingService, IMatchingRequestRepository matchingRequestRepository, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _applicationContext = applicationContext;
            _matchingService = matchingService;
            _matchingRequestRepository = matchingRequestRepository;
            _userManager = userManager;
        }

        // 1. Dashboard: Displays the student's submitted proposals
        public async Task<IActionResult> Dashboard()
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposals = await _context.ProjectProposals
                .Where(p => p.StudentId == studentId)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            var matchedSupervisorIds = proposals
                .Where(p => p.Status == ProjectStatus.Matched && p.IsIdentityRevealed && !string.IsNullOrEmpty(p.SupervisorId))
                .Select(p => p.SupervisorId!)
                .Distinct()
                .ToList();

            var supervisors = await _applicationContext.Users
                .Where(u => matchedSupervisorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var dashboardModels = proposals.Select(p => new StudentProjectProposalViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea?.Name,
                Status = p.Status,
                SupervisorId = p.SupervisorId,
                SupervisorName = p.Status == ProjectStatus.Matched && p.IsIdentityRevealed && !string.IsNullOrEmpty(p.SupervisorId)
                    ? supervisors.GetValueOrDefault(p.SupervisorId!)?.FullName
                    : null,
                SupervisorEmail = p.Status == ProjectStatus.Matched && p.IsIdentityRevealed && !string.IsNullOrEmpty(p.SupervisorId)
                    ? supervisors.GetValueOrDefault(p.SupervisorId!)?.Email
                    : null,
                CreatedAt = p.CreatedAt,
                MatchedAt = p.MatchedAt,
                LastModifiedAt = p.LastModifiedAt
            }).ToList();

            return View(dashboardModels);
        }

        /// <summary>
        /// GET: Student/FindMatches/{proposalId}
        /// Finds best matching supervisors for a specific project proposal
        /// Uses proposal abstract + technical stack + research area for matching
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> FindMatches(int proposalId)
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            // Verify proposal belongs to current student
            var proposal = await _context.ProjectProposals.FindAsync(proposalId);
            if (proposal == null || proposal.StudentId != studentId)
            {
                return NotFound();
            }

            // Get best matching supervisors based on proposal content
            var matches = await _matchingService.GetBestMatchesForProposal(proposalId);

            // Check which supervisors the student has already applied to for this proposal
            var existingRequests = await _matchingRequestRepository.GetByStudentIdAsync(studentId);
            var appliedSupervisorIds = existingRequests
                .Where(r => r.ProposalId == proposalId)
                .Select(r => r.SupervisorId)
                .ToHashSet();

            // Store the proposalId in ViewBag for the view
            ViewBag.ProposalId = proposalId;
            ViewBag.ProposalTitle = proposal.Title;
            ViewBag.MatchCount = matches.Count;
            ViewBag.AppliedSupervisorIds = appliedSupervisorIds;

            return View(matches);
        }

        /// <summary>
        /// POST: Student/ApplyToSupervisor
        /// Student applies to a supervisor for a specific proposal
        /// Creates a matching request and saves it to the database
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ApplyToSupervisor(string supervisorId, int proposalId)
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            Console.WriteLine($"DEBUG: ApplyToSupervisor called - StudentId: {studentId}, SupervisorId: {supervisorId}, ProposalId: {proposalId}");

            // Verify proposal belongs to current student
            var proposal = await _context.ProjectProposals.FindAsync(proposalId);
            if (proposal == null || proposal.StudentId != studentId)
            {
                Console.WriteLine($"DEBUG: Invalid proposal - proposal is null: {proposal == null}, student mismatch: {proposal?.StudentId != studentId}");
                TempData["ErrorMessage"] = "Invalid proposal.";
                return RedirectToAction("Dashboard");
            }

            // Check if already applied to this supervisor for this proposal
            var existingRequest = await _matchingRequestRepository.GetByProposalAndSupervisorAsync(proposalId, supervisorId);
            if (existingRequest != null)
            {
                Console.WriteLine($"DEBUG: Duplicate application found - existing request ID: {existingRequest.Id}");
                TempData["ErrorMessage"] = "You have already applied to this supervisor for this proposal.";
                return RedirectToAction("FindMatches", new { proposalId });
            }

            try
            {
                var matchingRequest = new MatchingRequest
                {
                    StudentId = studentId,
                    SupervisorId = supervisorId,
                    ProposalId = proposalId,
                    Status = MatchingRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"DEBUG: Creating matching request - StudentId: {matchingRequest.StudentId}, SupervisorId: {matchingRequest.SupervisorId}, ProposalId: {matchingRequest.ProposalId}");

                await _matchingRequestRepository.AddAsync(matchingRequest);
                Console.WriteLine($"DEBUG: Matching request saved successfully with ID: {matchingRequest.Id}");

                TempData["SuccessMessage"] = "Your application has been sent to the supervisor!";
                return RedirectToAction("MyRequests", "Matching");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error applying to supervisor: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while submitting your application.";
                return RedirectToAction("FindMatches", new { proposalId });
            }
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
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitProposal(ProjectProposal model)
        {
            var userIdString = _userManager.GetUserId(User);

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

        // GET: Student/EditProposal/5
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditProposal(int id)
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposal = await _context.ProjectProposals.FindAsync(id);
            if (proposal == null || proposal.StudentId != studentId)
            {
                return NotFound();
            }

            if (proposal.Status != ProjectStatus.Pending || !string.IsNullOrEmpty(proposal.SupervisorId))
            {
                TempData["ErrorMessage"] = "Only proposals that have not been matched can be edited.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name", proposal.ResearchAreaId);
            return View(proposal);
        }

        // POST: Student/EditProposal/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditProposal(ProjectProposal model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposal = await _context.ProjectProposals.FindAsync(model.Id);
            if (proposal == null || proposal.StudentId != studentId)
            {
                return NotFound();
            }

            if (proposal.Status != ProjectStatus.Pending || !string.IsNullOrEmpty(proposal.SupervisorId))
            {
                TempData["ErrorMessage"] = "Only proposals that have not been matched can be edited.";
                return RedirectToAction("Dashboard");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ResearchAreas = new SelectList(await _context.ResearchAreas.ToListAsync(), "Id", "Name", model.ResearchAreaId);
                return View(model);
            }

            proposal.Title = model.Title;
            proposal.Abstract = model.Abstract;
            proposal.TechnicalStack = model.TechnicalStack;
            proposal.ResearchAreaId = model.ResearchAreaId;
            proposal.LastModifiedAt = DateTime.UtcNow;

            _context.ProjectProposals.Update(proposal);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proposal updated successfully.";
            return RedirectToAction("Dashboard");
        }

        // POST: Student/WithdrawProposal/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> WithdrawProposal(int id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var proposal = await _context.ProjectProposals.FindAsync(id);
            if (proposal == null || proposal.StudentId != studentId)
            {
                return NotFound();
            }

            if (proposal.Status != ProjectStatus.Pending || !string.IsNullOrEmpty(proposal.SupervisorId))
            {
                TempData["ErrorMessage"] = "Only proposals that have not been matched can be withdrawn.";
                return RedirectToAction("Dashboard");
            }

            var requests = _applicationContext.MatchingRequests.Where(r => r.ProposalId == id);
            _applicationContext.MatchingRequests.RemoveRange(requests);
            _context.ProjectProposals.Remove(proposal);
            await _applicationContext.SaveChangesAsync();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proposal withdrawn successfully.";
            return RedirectToAction("Dashboard");
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