using System;
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
        private readonly CustomDbContext _customContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, CustomDbContext customContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _customContext = customContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // Get all users and their roles
            var allUsers = await _userManager.Users.ToListAsync();
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            // Calculate user distribution
            viewModel.UserDistribution.Students = students.Count;
            viewModel.UserDistribution.Supervisors = supervisors.Count;
            viewModel.UserDistribution.Admins = admins.Count;
            viewModel.UserDistribution.Unassigned = allUsers.Count - students.Count - supervisors.Count - admins.Count;

            // Get system statistics
            viewModel.Statistics.TotalStudents = students.Count;
            viewModel.Statistics.TotalSupervisors = supervisors.Count;
            viewModel.Statistics.TotalAdmins = admins.Count;
            viewModel.Statistics.TotalUsers = allUsers.Count;
            viewModel.Statistics.TotalResearchAreas = await _customContext.ResearchAreas.CountAsync();

            var allProposals = await _customContext.ProjectProposals.ToListAsync();
            viewModel.Statistics.TotalProposals = allProposals.Count;
            viewModel.Statistics.PendingProposals = allProposals.Count(p => p.Status == ProjectStatus.Pending);
            viewModel.Statistics.MatchedProposals = allProposals.Count(p => p.Status == ProjectStatus.Matched);
            viewModel.Statistics.TotalMatches = viewModel.Statistics.MatchedProposals;
            viewModel.Statistics.ApprovedProposals = allProposals.Count(p => p.Status == ProjectStatus.Approved);
            viewModel.Statistics.RejectedProposals = allProposals.Count(p => p.Status == ProjectStatus.Rejected);

            viewModel.Statistics.PendingRequests = await _context.MatchingRequests.CountAsync(r => r.Status == MatchingRequestStatus.Pending);

            if (viewModel.Statistics.TotalProposals > 0)
            {
                viewModel.Statistics.MatchRate = (double)viewModel.Statistics.MatchedProposals / viewModel.Statistics.TotalProposals * 100;
            }

            // Get recent matches (last 5)
            var recentMatches = await _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Matched)
                .Include(p => p.ResearchArea)
                .OrderByDescending(p => p.MatchedAt)
                .Take(5)
                .ToListAsync();

            var userNameMap = allUsers.ToDictionary(u => u.Id, u => u.FullName);
            foreach (var match in recentMatches)
            {
                var studentName = userNameMap.ContainsKey(match.StudentId) ? userNameMap[match.StudentId] : "Unknown";
                var supervisorName = !string.IsNullOrEmpty(match.SupervisorId) && userNameMap.ContainsKey(match.SupervisorId)
                    ? userNameMap[match.SupervisorId] : "Unassigned";

                viewModel.RecentMatches.Add(new RecentMatchDto
                {
                    ProposalId = match.Id,
                    ProjectTitle = match.Title,
                    StudentName = studentName,
                    SupervisorName = supervisorName,
                    ResearchArea = match.ResearchArea?.Name ?? "N/A",
                    MatchedAt = match.MatchedAt
                });
            }

            // Get pending proposals (oldest first, limit to 10)
            var pendingProposals = await _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Pending)
                .Include(p => p.ResearchArea)
                .OrderBy(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var proposal in pendingProposals)
            {
                var studentName = userNameMap.ContainsKey(proposal.StudentId) ? userNameMap[proposal.StudentId] : "Unknown";

                viewModel.PendingProposals.Add(new PendingProposalDto
                {
                    ProposalId = proposal.Id,
                    Title = proposal.Title,
                    StudentName = studentName,
                    ResearchArea = proposal.ResearchArea?.Name ?? "N/A",
                    CreatedAt = proposal.CreatedAt,
                    DaysPending = (DateTime.UtcNow - proposal.CreatedAt).Days
                });
            }

            // Get research areas with statistics
            var researchAreas = await _customContext.ResearchAreas.ToListAsync();
            foreach (var area in researchAreas)
            {
                var areaProposals = allProposals.Where(p => p.ResearchAreaId == area.Id).ToList();
                viewModel.ResearchAreas.Add(new ResearchAreaDto
                {
                    Id = area.Id,
                    Name = area.Name,
                    TotalProposals = areaProposals.Count,
                    PendingProposals = areaProposals.Count(p => p.Status == ProjectStatus.Pending),
                    MatchedProposals = areaProposals.Count(p => p.Status == ProjectStatus.Matched),
                    CreatedAt = area.CreatedAt
                });
            }

            return View("Index", viewModel);
        }

        // GET: Admin/Dashboard (Legacy redirect)
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index");
        }

        // ==================== RESEARCH AREAS MANAGEMENT ====================

        // GET: Admin/ResearchAreas
        public async Task<IActionResult> ResearchAreas()
        {
            var areas = await _customContext.ResearchAreas.ToListAsync();
            var allProposals = await _customContext.ProjectProposals.ToListAsync();

            // Calculate statistics for each research area
            var areasWithStats = areas.Select(area =>
            {
                var areaProposals = allProposals.Where(p => p.ResearchAreaId == area.Id).ToList();
                return new ResearchAreaWithStats
                {
                    Id = area.Id,
                    Name = area.Name,
                    CreatedAt = area.CreatedAt,
                    TotalProposals = areaProposals.Count,
                    PendingProposals = areaProposals.Count(p => p.Status == ProjectStatus.Pending),
                    MatchedProposals = areaProposals.Count(p => p.Status == ProjectStatus.Matched)
                };
            }).ToList();

            ViewBag.TotalAreas = areas.Count;
            ViewBag.TotalProposals = allProposals.Count;
            ViewBag.TotalMatches = allProposals.Count(p => p.Status == ProjectStatus.Matched);

            return View(areasWithStats);
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
                    _customContext.Add(model);
                    await _customContext.SaveChangesAsync();
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
            var area = await _customContext.ResearchAreas.FindAsync(id);
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
                    _customContext.Update(model);
                    await _customContext.SaveChangesAsync();
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
            var area = await _customContext.ResearchAreas.FindAsync(id);
            if (area == null)
                return NotFound();

            try
            {
                _customContext.ResearchAreas.Remove(area);
                await _customContext.SaveChangesAsync();
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
            List<ApplicationUser> users = new List<ApplicationUser>();

            if (string.IsNullOrEmpty(role))
            {
                users = await _userManager.Users.ToListAsync();
            }
            else
            {
                users = (await _userManager.GetUsersInRoleAsync(role)).ToList();
            }

            // Get roles for each user
            var usersWithRoles = new List<UserWithRole>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserWithRole
                {
                    User = user,
                    Roles = userRoles.ToList()
                });
            }

            ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
            ViewBag.SelectedRole = role;
            return View("ManageUsers", usersWithRoles);
        }

        // GET: Admin/Users/Create
        public IActionResult CreateUser()
        {
            ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                UserRole = model.Role,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["SuccessMessage"] = "User account created successfully.";
            return RedirectToAction("Users");
        }

        // GET: Admin/Users/Edit/{userId}
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = userRoles.FirstOrDefault() ?? "Student"
            };

            ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
            return View(model);
        }

        // POST: Admin/Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            user.FullName = model.FullName;
            user.UserRole = model.Role;

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
                return View(model);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = new[] { "Student", "Supervisor", "Admin" };
                return View(model);
            }

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction("Users");
        }

        // GET: Admin/Requests
        public async Task<IActionResult> Requests()
        {
            var requests = await _context.MatchingRequests.ToListAsync();
            var allUsers = await _userManager.Users.ToListAsync();
            var userNameMap = allUsers.ToDictionary(u => u.Id, u => u.FullName);
            var proposals = await _customContext.ProjectProposals.ToListAsync();
            var proposalTitleMap = proposals.ToDictionary(p => p.Id, p => p.Title);

            var requestDtos = requests.Select(r => new RequestMonitorDto
            {
                Id = r.Id,
                ProposalTitle = proposalTitleMap.ContainsKey(r.ProposalId) ? proposalTitleMap[r.ProposalId] : "Unknown Proposal",
                StudentName = userNameMap.ContainsKey(r.StudentId) ? userNameMap[r.StudentId] : "Unknown Student",
                SupervisorName = userNameMap.ContainsKey(r.SupervisorId) ? userNameMap[r.SupervisorId] : "Unknown Supervisor",
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).OrderByDescending(r => r.CreatedAt).ToList();

            return View(requestDtos);
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

        // POST: Admin/Users/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            try
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }
            return RedirectToAction("Users");
        }

        // ==================== MATCHES MANAGEMENT ====================

        // GET: Admin/Matches
        public async Task<IActionResult> Matches()
        {
            var matches = await _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Matched)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            // Fetch all student and supervisor names to display
            var allUsers = await _userManager.Users.ToListAsync();
            var nameMap = allUsers.ToDictionary(u => u.Id, u => u.FullName);

            ViewBag.StudentNameMap = nameMap;
            ViewBag.SupervisorNameMap = nameMap;
            ViewBag.Supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            ViewBag.MatchedCount = matches.Count;

            return View("ManageAllocations", matches);
        }

        // GET: Admin/AllMatches
        // Alternative view that lists all matched proposals with student/supervisor details
        public async Task<IActionResult> AllMatches()
        {
            var matches = await _customContext.ProjectProposals
                .Where(p => p.Status == ProjectStatus.Matched)
                .Include(p => p.ResearchArea)
                .ToListAsync();

            // Fetch all student and supervisor names to display
            var allUsers = await _userManager.Users.ToListAsync();
            var nameMap = allUsers.ToDictionary(u => u.Id, u => u.FullName);

            ViewBag.StudentNameMap = nameMap;
            ViewBag.SupervisorNameMap = nameMap;
            ViewBag.MatchedCount = matches.Count;

            return View("Matches", matches);
        }

        // POST: Admin/Matches/Reassign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int proposalId, string newSupervisorId)
        {
            var proposal = await _customContext.ProjectProposals.FindAsync(proposalId);
            if (proposal == null)
                return NotFound();

            var supervisor = await _userManager.FindByIdAsync(newSupervisorId);
            if (supervisor == null || !await _userManager.IsInRoleAsync(supervisor, "Supervisor"))
                return BadRequest("Supervisor not found or invalid supervisor role.");

            proposal.SupervisorId = newSupervisorId;
            proposal.LastModifiedAt = DateTime.UtcNow;

            try
            {
                _customContext.Update(proposal);
                await _customContext.SaveChangesAsync();
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
