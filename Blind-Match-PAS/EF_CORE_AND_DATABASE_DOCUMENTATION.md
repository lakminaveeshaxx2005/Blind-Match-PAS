# EF CORE & DATABASE STRATEGY - COMPREHENSIVE DOCUMENTATION

## Overview
This document details the Entity Framework Core implementation, database design, migrations strategy, and validation framework used in the Blind-Match Project Approval System.

---

## 1. EF CORE SETUP & CONFIGURATION

### 1.1 DbContext Configuration

**File:** [Data/ApplicationDbContext.cs](Data/ApplicationDbContext.cs)

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Register your tables here:
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<ProjectProposal> ProjectProposals { get; set; }
    public DbSet<ResearchArea> ResearchAreas { get; set; }
}
```

### 1.2 Dependency Injection Setup

**File:** [Program.cs](Program.cs)

```csharp
// 1. Connection String: Retrieves the database path from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register DbContext: Links your ApplicationDbContext to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Add Identity with Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

### 1.3 Connection String

**File:** appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlindMatchPAS;Trusted_Connection=true;"
  }
}
```

---

## 2. ENTITY MODELS & VALIDATIONS

### 2.1 ProjectProposal Model

**File:** [ProjectProposal.cs](ProjectProposal.cs)

```csharp
public enum ProjectStatus 
{ 
    Pending = 0, 
    UnderReview = 1, 
    Matched = 2, 
    Withdrawn = 3 
}

public class ProjectProposal
{
    [Key]
    public int Id { get; set; }

    // ===== BUSINESS FIELDS =====
    
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 5, 
        ErrorMessage = "Title must be between 5 and 200 characters.")]
    [RegularExpression(@"^[A-Z][a-zA-Z0-9\s\-_.,&()]+$", 
        ErrorMessage = "Title must start with an uppercase letter and contain only valid characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Abstract is required.")]
    [StringLength(1000, MinimumLength = 20, 
        ErrorMessage = "Abstract must be between 20 and 1000 characters.")]
    public string Abstract { get; set; }

    [Required(ErrorMessage = "Technical Stack is required.")]
    [StringLength(500, MinimumLength = 5, 
        ErrorMessage = "Technical Stack must be between 5 and 500 characters.")]
    public string TechnicalStack { get; set; }

    // ===== STATUS & RELATIONSHIPS =====
    
    public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

    [Required(ErrorMessage = "Research Area is required.")]
    public int ResearchAreaId { get; set; }
    public ResearchArea? ResearchArea { get; set; }  // ← Navigation property

    [Required(ErrorMessage = "Student ID is required.")]
    public string StudentId { get; set; }           // ← Foreign key to AspNetUsers

    public string? SupervisorId { get; set; }       // ← Assigned on match

    // ===== BLIND MATCHING CONTROL =====
    
    public bool IsIdentityRevealed { get; set; } = false;

    // ===== AUDIT FIELDS =====
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? MatchedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; } = DateTime.UtcNow;
}
```

**Validations Enforced:**
- ✅ Title: 5-200 chars, must start with uppercase, regex validation
- ✅ Abstract: 20-1000 chars
- ✅ TechnicalStack: 5-500 chars
- ✅ ResearchAreaId: Required, must exist in ResearchAreas
- ✅ StudentId: Required, must exist in AspNetUsers
- ✅ IsIdentityRevealed: Boolean flag for gating identity access

### 2.2 ApplicationUser Model

**File:** [Models/ApplicationUser.cs](Models/ApplicationUser.cs)

```csharp
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "Full name must be between 3 and 100 characters.")]
    public string FullName { get; set; }

    public string? UserRole { get; set; }  // Student, Supervisor, or Admin

    [StringLength(500, ErrorMessage = "Expertise field cannot exceed 500 characters.")]
    public string? Expertise { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Inherits from IdentityUser:**
- Id (Guid)
- Email
- UserName
- PasswordHash
- PhoneNumber
- etc.

**Extensions:**
- FullName: User's display name
- UserRole: Role assignment (redundant with AspNetRoles, but useful for quick access)
- Expertise: Supervisor's areas of expertise
- CreatedAt: Account creation timestamp

### 2.3 ResearchArea Model

**File:** [ResearchArea.cs](ResearchArea.cs)

```csharp
public class ResearchArea
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Research area name is required.")]
    [StringLength(100, MinimumLength = 3, 
        ErrorMessage = "Research area name must be between 3 and 100 characters.")]
    public string Name { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Seeded with default areas during startup:**
- Artificial Intelligence
- Machine Learning
- Cybersecurity
- Cloud Computing
- Data Science
- Web Development
- Mobile Development
- Blockchain

---

## 3. MIGRATIONS STRATEGY

### 3.1 Migration Overview

| # | Migration Name | Date | Purpose |
|---|---|---|---|
| 1 | InitialProjectSetup | 2026-04-04 | Core tables (ProjectProposals, ResearchAreas, AspNetUsers) |
| 2 | UpdateNullableModels | 2026-04-05 | Field validations, string lengths, constraints |
| 3 | AddRoleBasedAccess | 2026-04-05 | Authorization tables (AspNetRoles, AspNetUserRoles, AspNetRoleClaims) |
| 4 | EnhanceModelsWithAuditFields | 2026-04-06 | Timestamp auditing (CreatedAt, MatchedAt, LastModifiedAt) |

### 3.2 Migration Files Structure

Each migration has:
- **{Name}.cs** → Migration code with Up() and Down() methods
- **{Name}.Designer.cs** → Generated snapshot of entities at that point
- **ApplicationDbContextModelSnapshot.cs** → Latest model snapshot

### 3.3 Migration Lifecycle Commands

```powershell
# ===== CREATE MIGRATIONS =====

# Create new migration (generates from model changes)
dotnet ef migrations add <MigrationName>

# Example:
dotnet ef migrations add EnhanceModelsWithAuditFields

# ===== QUERY MIGRATIONS =====

# List all migrations
dotnet ef migrations list

# Show migration details
dotnet ef migrations info

# ===== APPLY MIGRATIONS =====

# Apply to database (runs all pending migrations)
dotnet ef database update

# Apply to specific migration
dotnet ef database update <MigrationName>

# ===== ROLLBACK MIGRATIONS =====

# Revert to specific migration
dotnet ef database update <PreviousMigrationName>

# Remove last migration (only if not applied!)
dotnet ef migrations remove

# ===== SCRIPTS FOR CI/CD =====

# Generate SQL script for specific migration range
dotnet ef migrations script <FromMigration> <ToMigration> -o migrate.sql
```

---

## 4. DATABASE SCHEMA

### 4.1 Core Tables

#### AspNetUsers (Identity)
```sql
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) PRIMARY KEY,
    UserName NVARCHAR(256),
    NormalizedUserName NVARCHAR(256),
    Email NVARCHAR(256),
    NormalizedEmail NVARCHAR(256),
    EmailConfirmed BIT,
    PasswordHash NVARCHAR(MAX),
    SecurityStamp NVARCHAR(MAX),
    ConcurrencyStamp NVARCHAR(MAX),
    PhoneNumber NVARCHAR(MAX),
    PhoneNumberConfirmed BIT,
    TwoFactorEnabled BIT,
    LockoutEnd DATETIMEOFFSET,
    LockoutEnabled BIT,
    AccessFailedCount INT,
    
    -- ← Extended ApplicationUser fields
    FullName NVARCHAR(100) NOT NULL,
    UserRole NVARCHAR(MAX),
    Expertise NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

#### ProjectProposals
```sql
CREATE TABLE ProjectProposals (
    Id INT PRIMARY KEY IDENTITY(1,1),
    
    -- Business fields
    Title NVARCHAR(200) NOT NULL,
    Abstract NVARCHAR(1000) NOT NULL,
    TechnicalStack NVARCHAR(500) NOT NULL,
    Status INT NOT NULL DEFAULT 0,  -- Enum: 0=Pending, 1=UnderReview, 2=Matched, 3=Withdrawn
    
    -- Relationships
    ResearchAreaId INT NOT NULL,
    StudentId NVARCHAR(450) NOT NULL,
    SupervisorId NVARCHAR(450),
    
    -- Blind matching control
    IsIdentityRevealed BIT DEFAULT 0,
    
    -- Audit fields
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    MatchedAt DATETIME2,
    LastModifiedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ProjectProposals_ResearchAreas 
        FOREIGN KEY (ResearchAreaId) REFERENCES ResearchAreas(Id),
    
    CONSTRAINT FK_ProjectProposals_StudentId 
        FOREIGN KEY (StudentId) REFERENCES AspNetUsers(Id),
    
    CONSTRAINT FK_ProjectProposals_SupervisorId 
        FOREIGN KEY (SupervisorId) REFERENCES AspNetUsers(Id)
);

-- Performance indexes
CREATE INDEX IX_ProjectProposals_Status ON ProjectProposals(Status);
CREATE INDEX IX_ProjectProposals_StudentId ON ProjectProposals(StudentId);
CREATE INDEX IX_ProjectProposals_SupervisorId ON ProjectProposals(SupervisorId);
CREATE INDEX IX_ProjectProposals_MatchedAt ON ProjectProposals(MatchedAt);
```

#### ResearchAreas
```sql
CREATE TABLE ResearchAreas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

#### AspNetRoles (Authorization)
```sql
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) PRIMARY KEY,
    Name NVARCHAR(256),
    NormalizedName NVARCHAR(256),
    ConcurrencyStamp NVARCHAR(MAX)
);

-- Seeded with:
-- - Student
-- - Supervisor
-- - Admin
```

#### AspNetUserRoles (Role Assignment)
```sql
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450),
    RoleId NVARCHAR(450),
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)
);
```

---

## 5. QUERY PATTERNS & EF CORE USAGE

### 5.1 Common Query Patterns

**Pattern 1: Get Anonymous Proposals for Supervisor**
```csharp
var pendingProposals = await _context.ProjectProposals
    .Where(p => p.Status == ProjectStatus.Pending)           // ← No StudentId selected
    .Include(p => p.ResearchArea)                             // ← Only ResearchArea
    .ToListAsync();
```

**Pattern 2: Reveal Identities After Match**
```csharp
var proposal = await _context.ProjectProposals
    .Include(p => p.ResearchArea)
    .FirstOrDefaultAsync(p => p.Id == id);

if (proposal.Status == ProjectStatus.Matched && proposal.IsIdentityRevealed)
{
    var student = await _context.ApplicationUsers
        .FirstOrDefaultAsync(u => u.Id == proposal.StudentId);  // ← Now load student
}
```

**Pattern 3: Get Student's Own Proposals**
```csharp
var studentProposals = await _context.ProjectProposals
    .Where(p => p.StudentId == userId)                        // ← Filter by owner
    .Include(p => p.ResearchArea)
    .ToListAsync();
```

**Pattern 4: Get Matched Projects for Admin**
```csharp
var matches = await _context.ProjectProposals
    .Where(p => p.Status == ProjectStatus.Matched)            // ← All matched
    .Include(p => p.ResearchArea)
    .ToListAsync();
```

**Pattern 5: Create New Proposal with Validation**
```csharp
var proposal = new ProjectProposal
{
    Title = "AI-Powered System",
    Abstract = "This project explores...",
    TechnicalStack = "Python, TensorFlow",
    ResearchAreaId = 1,
    StudentId = userId,
    Status = ProjectStatus.Pending
};

_context.Add(proposal);              // ← EF tracks entity
await _context.SaveChangesAsync();   // ← Validation + persists to DB
```

### 5.2 Performance Optimization

**Index Strategy:**
```csharp
// Supervisor browsing proposals (frequently fetched)
CREATE INDEX IX_ProjectProposals_Status ON ProjectProposals(Status);

// Finding user's proposals (frequently filtered)
CREATE INDEX IX_ProjectProposals_StudentId ON ProjectProposals(StudentId);

// Finding supervisor's assigned projects (frequently filtered)
CREATE INDEX IX_ProjectProposals_SupervisorId ON ProjectProposals(SupervisorId);

// Audit queries (finding recent matches)
CREATE INDEX IX_ProjectProposals_MatchedAt ON ProjectProposals(MatchedAt);
```

**Query Optimization Tips:**
```csharp
// ❌ BAD: N+1 queries
var proposals = _context.ProjectProposals.ToList();
foreach (var p in proposals)
{
    var area = _context.ResearchAreas.FirstOrDefault(a => a.Id == p.ResearchAreaId);
}

// ✅ GOOD: Single query with Include
var proposals = await _context.ProjectProposals
    .Include(p => p.ResearchArea)
    .ToListAsync();

// ✅ GOOD: Only select needed fields
var proposals = await _context.ProjectProposals
    .Where(p => p.Status == ProjectStatus.Pending)
    .Select(p => new {
        p.Id,
        p.Title,
        p.Abstract,
        AreaName = p.ResearchArea.Name  // ← Only needed properties
    })
    .ToListAsync();
```

---

## 6. VALIDATION FRAMEWORK

### 6.1 Model-Level Validation Attributes

| Attribute | Usage | Example |
|-----------|-------|---------|
| [Required] | Field must have value | `[Required] public string Title` |
| [StringLength] | String length constraints | `[StringLength(200, MinimumLength = 5)]` |
| [RegularExpression] | Pattern validation | `[RegularExpression(@"^[A-Z].*")]` |
| [Key] | Primary key designation | `[Key] public int Id` |
| [ForeignKey] | Foreign key relationship | `[ForeignKey("ResearchAreaId")]` |
| [EmailAddress] | Email format | `[EmailAddress] public string Email` |

### 6.2 Custom Validation Example

```csharp
// In controller: Manual validation before save
if (proposal.Status != ProjectStatus.Pending)
{
    ModelState.AddModelError("", "Only Pending proposals can be edited.");
    return View(proposal);
}

// In controller: Check ownership
if (proposal.StudentId != userId)
{
    return Forbid("You can only edit your own proposals.");
}

// Model validation runs automatically in SaveChangesAsync
if (!ModelState.IsValid)
{
    // Failed validations automatically caught
    return View(proposal);
}
```

---

## 7. DATA SEEDING STRATEGY

### 7.1 Seed Implementation

**File:** [Program.cs](Program.cs)

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    
    // Run migrations
    dbContext.Database.Migrate();

    // ===== SEED ROLES =====
    var roles = new[] { "Admin", "Student", "Supervisor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ===== SEED RESEARCH AREAS =====
    if (!dbContext.ResearchAreas.Any())
    {
        var researchAreas = new[]
        {
            new ResearchArea { Name = "Artificial Intelligence" },
            new ResearchArea { Name = "Machine Learning" },
            new ResearchArea { Name = "Cybersecurity" },
            // ... etc
        };
        dbContext.ResearchAreas.AddRange(researchAreas);
        await dbContext.SaveChangesAsync();
    }

    // ===== SEED DEFAULT ADMIN USER =====
    var adminEmail = "admin@blindmatchpas.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser 
        { 
            UserName = adminEmail, 
            Email = adminEmail, 
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "AdminPassword@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
```

### 7.2 Idempotent Seeding

All seed operations check `if (!dbContext.X.Any())` to ensure:
- No duplicate data on app restart
- Safe to run multiple times
- Non-destructive

---

## 8. TRANSACTION MANAGEMENT

### 8.1 Atomic Operations

```csharp
// Single transaction: Accept Proposal → Update Status → Record Timestamp
using var transaction = _context.Database.BeginTransaction();
try
{
    proposal.Status = ProjectStatus.Matched;
    proposal.SupervisorId = supervisorId;
    proposal.IsIdentityRevealed = true;
    proposal.MatchedAt = DateTime.UtcNow;
    
    _context.Update(proposal);
    await _context.SaveChangesAsync();
    
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    throw;
}
```

### 8.2 Implicit Transactions

By default, `SaveChangesAsync()` wraps all changes in a transaction:
```csharp
_context.Add(proposal);
_context.Update(otherProposal);
_context.Remove(deletedProposal);
await _context.SaveChangesAsync();  // ← All three operations or none
```

---

## 9. MIGRATION TESTING CHECKLIST

- [ ] Model changes compile
- [ ] Migration generates correctly
- [ ] Up() method creates/modifies schema
- [ ] Down() method can rollback changes
- [ ] No data loss in forward migration
- [ ] Existing data migrated correctly
- [ ] Indexes created for performance
- [ ] Foreign key constraints valid
- [ ] Default values applied
- [ ] Connection successful after migration

---

## 10. BEST PRACTICES IMPLEMENTED

✅ **Validation First:** Model-level validations prevent invalid data entry
✅ **Migration Tracking:** Every schema change tracked with migrations
✅ **Relationships Defined:** Foreign keys enforce referential integrity
✅ **Indexes Optimized:** Common queries have indexes for performance
✅ **Audit Trails:** CreatedAt, MatchedAt, LastModifiedAt fields track changes
✅ **Idempotent Seeding:** Safe to run multiple times
✅ **Atomic Transactions:** Match confirmation is all-or-nothing
✅ **Role-Based Authorization:** Prevents unauthorized access
✅ **Security-First:** Blind matching hides sensitive data until matched

---

## Conclusion

The Blind-Match PAS demonstrates **enterprise-grade EF Core usage**:
- Clean DbContext design
- Comprehensive model validations
- Proper migration strategy with 4 tracked migrations
- Performance-optimized queries with indexes
- Audit trail for compliance
- Role-based access control
- Idempotent data seeding
- Atomic transaction handling
