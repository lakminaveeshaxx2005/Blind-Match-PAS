# Implementation Guide for Enhanced Models

This guide explains how to use the new validation methods and properties in your controllers and views.

## Table of Contents
1. [Using IsValidStateTransition() in Controllers](#using-isvalidstatetransition-in-controllers)
2. [Using CanEdit Property](#using-canedit-property)
3. [Using Role Helper Properties](#using-role-helper-properties)
4. [Using Display Attributes in Views](#using-display-attributes-in-views)
5. [Entity Framework Usage](#entity-framework-usage)
6. [Common Scenarios](#common-scenarios)

---

## Using IsValidStateTransition() in Controllers

### Purpose
Ensures project status changes follow the blind matching workflow:
```
Pending → UnderReview → Matched
       ↓
      Pending (rejection)
```

### Example 1: Supervisor Expresses Interest (Pending → UnderReview)

**Before** (without validation):
```csharp
[HttpPost]
public async Task<IActionResult> ExpressInterest(int proposalId)
{
    var proposal = await _db.ProjectProposals.FindAsync(proposalId);
    proposal.Status = ProjectStatus.UnderReview; // Blindly changes without validation
    await _db.SaveChangesAsync();
    return Ok();
}
```

**After** (with validation):
```csharp
[HttpPost]
public async Task<IActionResult> ExpressInterest(int proposalId)
{
    var proposal = await _db.ProjectProposals.FindAsync(proposalId);
    
    // Validate the state transition before making changes
    if (!proposal.IsValidStateTransition(ProjectStatus.UnderReview))
    {
        ModelState.AddModelError("", $"Cannot change status from {proposal.Status} to UnderReview");
        return BadRequest(ModelState);
    }
    
    proposal.Status = ProjectStatus.UnderReview;
    await _db.SaveChangesAsync();
    
    return RedirectToAction(nameof(Index));
}
```

### Example 2: Student Confirms Match (UnderReview → Matched)

```csharp
[HttpPost]
public async Task<IActionResult> ConfirmMatch(int proposalId)
{
    var proposal = await _db.ProjectProposals.FindAsync(proposalId);
    var supervisorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Validate transition and ownership
    if (!proposal.IsValidStateTransition(ProjectStatus.Matched))
    {
        return BadRequest("Invalid operation for this proposal status");
    }
    
    // Mark as matched and reveal identities
    proposal.Status = ProjectStatus.Matched;
    proposal.IsIdentityRevealed = true;
    proposal.MatchedAt = DateTime.UtcNow;
    proposal.SupervisorId = supervisorId;
    proposal.LastModifiedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    return RedirectToAction(nameof(MatchSuccess), new { id = proposalId });
}
```

### Example 3: Student Rejects Match (UnderReview → Pending)

```csharp
[HttpPost]
public async Task<IActionResult> RejectMatch(int proposalId)
{
    var proposal = await _db.ProjectProposals.FindAsync(proposalId);
    
    // Validate the rejection is from correct state
    if (!proposal.IsValidStateTransition(ProjectStatus.Pending))
    {
        return BadRequest("Can only reject from UnderReview status");
    }
    
    proposal.Status = ProjectStatus.Pending;
    proposal.SupervisorId = null; // Clear the supervisor reference
    proposal.LastModifiedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    // Return to pending state
    return RedirectToAction(nameof(Index));
}
```

### Example 4: Student Withdraws Proposal (Pending → Withdrawn)

```csharp
[HttpPost]
public async Task<IActionResult> WithdrawProposal(int proposalId)
{
    var proposal = await _db.ProjectProposals.FindAsync(proposalId);
    
    if (!proposal.IsValidStateTransition(ProjectStatus.Withdrawn))
    {
        return BadRequest("Can only withdraw proposals in Pending status");
    }
    
    proposal.Status = ProjectStatus.Withdrawn;
    proposal.LastModifiedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    return RedirectToAction(nameof(Index));
}
```

---

## Using CanEdit Property

### Purpose
Determines if a student can edit their proposal (only when Pending).

### Example 1: Check Before Showing Edit Button in View

**In Controller**:
```csharp
[HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var proposal = await _db.ProjectProposals.FindAsync(id);
    
    if (!proposal.CanEdit)
    {
        return BadRequest("This proposal cannot be edited");
    }
    
    return View(proposal);
}
```

**In View** (Razor):
```html
@model ProjectProposal

@if (Model.CanEdit)
{
    <a href="@Url.Action("Edit", new { id = Model.Id })" class="btn btn-primary">
        Edit
    </a>
}
else
{
    <span class="badge badge-secondary">Cannot edit (Status: @Model.Status)</span>
}
```

### Example 2: Protect Edit Post Action

```csharp
[HttpPost]
public async Task<IActionResult> Edit(int id, ProjectProposal updatedProposal)
{
    var proposal = await _db.ProjectProposals.FindAsync(id);
    
    // Extra validation layer
    if (!proposal.CanEdit)
    {
        return BadRequest("This proposal is under review or already matched");
    }
    
    // Update only editable fields
    proposal.Title = updatedProposal.Title;
    proposal.Abstract = updatedProposal.Abstract;
    proposal.TechnicalStack = updatedProposal.TechnicalStack;
    proposal.ResearchAreaId = updatedProposal.ResearchAreaId;
    proposal.LastModifiedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    return RedirectToAction(nameof(Index));
}
```

### Example 3: Dashboard Status Indicator

```csharp
public async Task<IActionResult> Dashboard()
{
    var userProposals = await _db.ProjectProposals
        .Where(p => p.StudentId == UserId)
        .Select(p => new
        {
            p.Id,
            p.Title,
            p.Status,
            CanEdit = p.CanEdit,
            CanReject = p.Status == ProjectStatus.UnderReview,
            CanWithdraw = p.Status == ProjectStatus.Pending
        })
        .ToListAsync();
    
    return View(userProposals);
}
```

---

## Using Role Helper Properties

### Purpose
Simplifies role-based authorization instead of string comparisons.

### Before (Without Helper Properties)
```csharp
var currentUser = await _userManager.GetUserAsync(User);

if (currentUser.UserRole == "Student")
{
    // Student-specific logic
}
else if (currentUser.UserRole == "Supervisor")
{
    // Supervisor-specific logic
}
else if (currentUser.UserRole == "Admin")
{
    // Admin-specific logic
}
```

### After (With Helper Properties)
```csharp
var currentUser = await _userManager.GetUserAsync(User);

if (currentUser.IsStudent)
{
    // Student-specific logic
}
else if (currentUser.IsSupervisor)
{
    // Supervisor-specific logic
}
else if (currentUser.IsAdmin)
{
    // Admin-specific logic
}
```

### Example 1: Student Dashboard

```csharp
[Authorize]
public async Task<IActionResult> StudentDashboard()
{
    var currentUser = await _userManager.GetUserAsync(User);
    
    if (!currentUser.IsStudent)
    {
        return Unauthorized();
    }
    
    var proposals = await _db.ProjectProposals
        .Where(p => p.StudentId == currentUser.Id)
        .Include(p => p.ResearchArea)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();
    
    return View(proposals);
}
```

### Example 2: Supervisor View

```csharp
[Authorize]
public async Task<IActionResult> SupervisorProposals()
{
    var currentUser = await _userManager.GetUserAsync(User);
    
    if (!currentUser.IsSupervisor)
    {
        return Unauthorized();
    }
    
    var proposals = await _db.ProjectProposals
        .Where(p => p.Status == ProjectStatus.Pending || p.Status == ProjectStatus.UnderReview)
        .Include(p => p.ResearchArea)
        .ToListAsync();
    
    return View(proposals);
}
```

### Example 3: Admin Only Section

```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> AdminDashboard()
{
    var users = await _userManager.Users.ToListAsync();
    var admins = users.Where(u => u.IsAdmin).ToList();
    var supervisors = users.Where(u => u.IsSupervisor).ToList();
    var students = users.Where(u => u.IsStudent).ToList();
    
    return View(new { Admins = admins, Supervisors = supervisors, Students = students });
}
```

### Example 4: Complex Authorization Logic

```csharp
[HttpPost]
public async Task<IActionResult> UpdateProposal(int id, ProjectProposal updated)
{
    var currentUser = await _userManager.GetUserAsync(User);
    var proposal = await _db.ProjectProposals.FindAsync(id);
    
    // Authorization rules:
    // - Students can edit only their own pending proposals
    // - Supervisors can view any proposal
    // - Admins can do anything
    
    if (currentUser.IsStudent)
    {
        if (proposal.StudentId != currentUser.Id || !proposal.CanEdit)
        {
            return Unauthorized();
        }
    }
    else if (currentUser.IsSupervisor)
    {
        // Supervisors can view but not edit directly
        return BadRequest("Supervisors cannot edit proposals");
    }
    else if (!currentUser.IsAdmin)
    {
        return Unauthorized("Invalid user role");
    }
    
    // Proceed with update...
    return Ok();
}
```

---

## Using Display Attributes in Views

### Purpose
Display attributes automatically provide labels and make UI consistent.

### Example 1: Automatic Label Display with DisplayFor

**View**:
```html
@model ProjectProposal

<div class="form-group">
    <label asp-for="Title"></label>
    <input asp-for="Title" class="form-control" />
    <span asp-validation-for="Title" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="Abstract"></label>
    <textarea asp-for="Abstract" class="form-control"></textarea>
    <span asp-validation-for="Abstract" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="TechnicalStack"></label>
    <input asp-for="TechnicalStack" class="form-control" />
    <span asp-validation-for="TechnicalStack" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="ResearchAreaId"></label>
    <select asp-for="ResearchAreaId" asp-items="ViewBag.ResearchAreas" class="form-control"></select>
    <span asp-validation-for="ResearchAreaId" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="Status"></label>
    <select asp-for="Status" asp-items="Html.GetEnumSelectList(typeof(ProjectStatus))" class="form-control"></select>
    <span asp-validation-for="Status" class="text-danger"></span>
</div>
```

This automatically includes labels:
- "Project Title" instead of "Title"
- "Project Abstract" instead of "Abstract"
- "Technical Stack" instead of "TechnicalStack"
- "Research Area" instead of "ResearchAreaId"
- "Status" instead of "Status"

### Example 2: Display Properties in Details View

```html
@model ProjectProposal

<div class="row">
    <div class="col-md-6">
        <h5>
            <label asp-for="Title"></label>
        </h5>
        <p>@Model.Title</p>
    </div>
    
    <div class="col-md-6">
        <h5>
            <label asp-for="Status"></label>
        </h5>
        <p>
            <span class="badge badge-info">@Model.Status</span>
        </p>
    </div>
</div>

<div class="row">
    <div class="col-12">
        <h5>
            <label asp-for="Abstract"></label>
        </h5>
        <p>@Model.Abstract</p>
    </div>
</div>

<div class="row">
    <div class="col-md-6">
        <h5>
            <label asp-for="TechnicalStack"></label>
        </h5>
        <p>@Model.TechnicalStack</p>
    </div>
    
    <div class="col-md-6">
        <h5>
            <label asp-for="CreatedAt"></label>
        </h5>
        <p>@Model.CreatedAt.ToString("yyyy-MM-dd HH:mm")</p>
    </div>
</div>
```

### Example 3: User Profile Display

```html
@model ApplicationUser

<div class="user-profile">
    <h3>
        <label asp-for="FullName"></label>
    </h3>
    <p>@Model.FullName</p>
    
    <h5>
        <label asp-for="UserRole"></label>
    </h5>
    <p>
        @if (Model.IsStudent)
        {
            <span class="badge badge-primary">Student</span>
        }
        else if (Model.IsSupervisor)
        {
            <span class="badge badge-success">Supervisor</span>
        }
        else if (Model.IsAdmin)
        {
            <span class="badge badge-danger">Admin</span>
        }
    </p>
    
    @if (!string.IsNullOrEmpty(Model.Expertise))
    {
        <h5>
            <label asp-for="Expertise"></label>
        </h5>
        <p>@Model.Expertise</p>
    }
    
    <h5>
        <label asp-for="CreatedAt"></label>
    </h5>
    <p>@Model.CreatedAt.ToString("MMMM dd, yyyy")</p>
</div>
```

---

## Entity Framework Usage

### Example 1: Loading Related Data

```csharp
// Load proposals with their research area
var proposals = await _db.ProjectProposals
    .Include(p => p.ResearchArea)
    .ToListAsync();

// Load research areas with their proposals
var areas = await _db.ResearchAreas
    .Include(r => r.ProjectProposals)
    .ToListAsync();
```

### Example 2: Query Examples Using New Properties

```csharp
// Find proposals that can still be edited
var editableProposals = await _db.ProjectProposals
    .Where(p => p.CanEdit) // Uses the computed property
    .ToListAsync();

// Find supervisors for matching
var supervisors = await _userManager.Users
    .Where(u => u.IsSupervisor) // Uses helper property
    .ToListAsync();

// Get proposals in each status for dashboard
var pendingProposals = await _db.ProjectProposals
    .Where(p => p.Status == ProjectStatus.Pending)
    .ToListAsync();

var underReviewProposals = await _db.ProjectProposals
    .Where(p => p.Status == ProjectStatus.UnderReview)
    .ToListAsync();
```

---

## Common Scenarios

### Scenario 1: Complete Matching Flow

```csharp
// Step 1: Supervisor views pending proposals
var pendingProposals = await _db.ProjectProposals
    .Where(p => p.Status == ProjectStatus.Pending)
    .Include(p => p.ResearchArea)
    .ToListAsync();

// Step 2: Supervisor expresses interest
var proposal = await _db.ProjectProposals.FindAsync(proposalId);
if (proposal.IsValidStateTransition(ProjectStatus.UnderReview))
{
    proposal.Status = ProjectStatus.UnderReview;
    proposal.LastModifiedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
}

// Step 3: Student reviews interest and confirms
var student = await _userManager.GetUserAsync(User);
if (student.IsStudent && proposal.Status == ProjectStatus.UnderReview)
{
    proposal.Status = ProjectStatus.Matched;
    proposal.SupervisorId = proposal.SupervisorId; // Already set by supervisor
    proposal.IsIdentityRevealed = true;
    proposal.MatchedAt = DateTime.UtcNow;
    proposal.LastModifiedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
}
```

### Scenario 2: Protect Edit Operations

```csharp
[HttpPost]
public async Task<IActionResult> SaveDraft(int id, ProjectProposal draft)
{
    var user = await _userManager.GetUserAsync(User);
    
    // Only students can edit
    if (!user.IsStudent)
    {
        return Unauthorized();
    }
    
    var proposal = await _db.ProjectProposals.FindAsync(id);
    
    // Only own proposals
    if (proposal.StudentId != user.Id)
    {
        return Unauthorized();
    }
    
    // Only pending proposals can be edited
    if (!proposal.CanEdit)
    {
        return BadRequest("This proposal is under review or already matched");
    }
    
    // Apply updates
    proposal.Title = draft.Title;
    proposal.Abstract = draft.Abstract;
    proposal.TechnicalStack = draft.TechnicalStack;
    proposal.LastModifiedAt = DateTime.UtcNow;
    
    await _db.SaveChangesAsync();
    
    return Ok("Draft saved successfully");
}
```

### Scenario 3: Multi-Proposal Management

```csharp
[Authorize]
public async Task<IActionResult> MyProposals()
{
    var user = await _userManager.GetUserAsync(User);
    
    IEnumerable<ProjectProposal> proposals;
    
    if (user.IsStudent)
    {
        // Show all student's proposals
        proposals = await _db.ProjectProposals
            .Where(p => p.StudentId == user.Id)
            .Include(p => p.ResearchArea)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    else if (user.IsSupervisor)
    {
        // Show proposals supervised and available
        proposals = await _db.ProjectProposals
            .Where(p => p.Status != ProjectStatus.Withdrawn)
            .Include(p => p.ResearchArea)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    else if (user.IsAdmin)
    {
        // Admin sees everything
        proposals = await _db.ProjectProposals
            .Include(p => p.ResearchArea)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    
    return View(proposals);
}
```

---

## Best Practices

1. **Always Validate State Transitions**: Use `IsValidStateTransition()` before changing status
2. **Check CanEdit**: Always verify `proposal.CanEdit` before allowing edits
3. **Use Role Properties**: Prefer `user.IsStudent` over `user.UserRole == "Student"`
4. **Include Navigation**: Use `.Include()` to load related data to avoid lazy loading
5. **Timestamp Updates**: Always update `LastModifiedAt` when modifying proposals
6. **Reveal Identities**: Set `IsIdentityRevealed = true` only when status becomes `Matched`
7. **Authorization Checks**: Always verify user permissions in both controllers and views

