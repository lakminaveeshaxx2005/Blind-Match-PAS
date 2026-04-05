using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;

namespace Blind_Match_PAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalStudents = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.TotalSupervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            ViewBag.PendingProposals = await _context.ProjectProposals.CountAsync(p => p.Status == ProjectStatus.Pending);
            ViewBag.MatchedProposals = await _context.ProjectProposals.CountAsync(p => p.Status == ProjectStatus.Matched);
            ViewBag.TotalResearchAreas = await _context.ResearchAreas.CountAsync();
            return View();
        }

        // ==================== RESEARCH AREAS MANAGEMENT ====================

        // GET: Admin/ResearchAreas
        public async Task<IActionResult> ResearchAreas()
        {
            var areas = await _context.ResearchAreas.ToListAsync();
            return View(areas);
        }

        // GET: Admin/ResearchAreas/Create
        public IActionResult CreateResearchArea()
        {
            return View();
        }

        // POST: Admin/ResearchAreas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResearchArea(ResearchArea model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Research Area created successfully!";
                    return RedirectToAction("ResearchAreas");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating research area: {ex.Message}");
                }
            }
            return View(model);
        }

        // GET: Admin/ResearchAreas/Edit/{id}
        public async Task<IActionResult> EditResearchArea(int id)
        {
            var area = await _context.ResearchAreas.FindAsync(id);
            if (area == null)
                return NotFound();
            return View(area);
        }

        // POST: Admin/ResearchAreas/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResearchArea(int id, ResearchArea model)
        {
            if (id != model.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Research Area updated successfully!";
                    return RedirectToAction("ResearchAreas");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating research area: {ex.Message}");
                }
            }
            return View(model);
        }

        // POST: Admin/ResearchAreas/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResearchArea(int id)
        {
            var area = await _context.ResearchAreas.FindAsync(id);
            if (area == null)
                return NotFound();

            try
            {
                _context.ResearchAreas.Remove(area);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Research Area deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting research area: {ex.Message}";
            }
            return RedirectToAction("ResearchAreas");
        }

        // ==================== USER MANAGEMENT ====================

        // GET: Admin/Users
        public async Task<IActionResult> Users(string role = "")
        {
            List<IdentityUser> users = new List<IdentityUser>();

            if (string.IsNullOrEmpty(role))
            {
                users = await _userManager.Users.ToListAsync();
            }
            else
            {
                users = (await _userManager.GetUsersInRoleAsync(role)).ToList();
            }

            ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
            ViewBag.SelectedRole = role;
            return View(users);
        }

        // GET: Admin/Users/AssignRole/{userId}
        public async Task<IActionResult> AssignRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
            ViewBag.UserRoles = userRoles;
            return View(user);
        }

        // POST: Admin/Users/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove all current roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add new role
            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            TempData["SuccessMessage"] = $"User role updated to '{role}' successfully!";
            return RedirectToAction("Users");
        }

        // ==================== MATCHES MANAGEMENT ====================

        // GET: Admin/Matches
        public async Task<IActionResult> Matches()
        {
            var matches = await _context.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Matched)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            return View(matches);
        }

        // POST: Admin/Matches/Reassign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int proposalId, string newSupervisorId)
        {
            var proposal = await _context.ProjectProposals.FindAsync(proposalId);
            if (proposal == null)
                return NotFound();

            var supervisor = await _userManager.FindByIdAsync(newSupervisorId);
            if (supervisor == null)
                return BadRequest("Supervisor not found.");

            proposal.SupervisorId = newSupervisorId;

            try
            {
                _context.Update(proposal);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project reassigned successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error reassigning project: {ex.Message}";
            }

            return RedirectToAction("Matches");
        }
    }
}
