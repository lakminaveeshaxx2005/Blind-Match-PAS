using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;
using Blind_Match_PAS.Authorization;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String: Retrieves the database path from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register DbContexts: Links your contexts to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<CustomDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Identity Setup: Uses the built-in IdentityUser for authentication with Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI(); // Add this line!

// Add HTTP Context Accessor for authorization handlers
builder.Services.AddHttpContextAccessor();

// Add Authorization Services with Policies and Custom Handlers
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("SupervisorOnly", policy => policy.RequireRole("Supervisor"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ProposalOwner", policy => policy.AddRequirements(new ProposalOwnershipRequirement()));
    options.AddPolicy("SupervisorMatching", policy => policy.AddRequirements(new SupervisorMatchingRequirement()));
});

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, ProposalOwnershipHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SupervisorMatchingHandler>();

// Register repositories and services
builder.Services.AddScoped<Blind_Match_PAS.Repositories.IMatchingRequestRepository, Blind_Match_PAS.Repositories.MatchingRequestRepository>();
builder.Services.AddScoped<Blind_Match_PAS.Services.IMatchingService, Blind_Match_PAS.Services.MatchingService>();

// 4. Add MVC Services: Enables Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Data: Automatically populate roles, research areas, and admin user on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var customDbContext = scope.ServiceProvider.GetRequiredService<CustomDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    //dbContext.Database.Migrate(); // Ensure identity schema is created on startup
    //customDbContext.Database.Migrate(); // Ensure custom tables are created on startup

    // Seed Roles
    var roles = new[] { "Admin", "Student", "Supervisor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed Research Areas
    if (!customDbContext.ResearchAreas.Any())
    {
        var researchAreas = new[]
        {
            new ResearchArea { Name = "Artificial Intelligence" },
            new ResearchArea { Name = "Machine Learning" },
            new ResearchArea { Name = "Cybersecurity" },
            new ResearchArea { Name = "Cloud Computing" },
            new ResearchArea { Name = "Data Science" },
            new ResearchArea { Name = "Web Development" },
            new ResearchArea { Name = "Mobile Development" },
            new ResearchArea { Name = "Blockchain" }
        };
        customDbContext.ResearchAreas.AddRange(researchAreas);
        await customDbContext.SaveChangesAsync();
    }

    // Seed Default Admin User
    var adminEmail = "admin@blindmatchpas.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Admin User",
            UserRole = "Admin"
        };
        await userManager.CreateAsync(adminUser, "AdminPassword@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Seed Student User
    var studentEmail = "student1@test.com";
    var studentUser = await userManager.FindByEmailAsync(studentEmail);
    if (studentUser == null)
    {
        studentUser = new ApplicationUser
        {
            UserName = studentEmail,
            Email = studentEmail,
            EmailConfirmed = true,
            FullName = "Test Student",
            UserRole = "Student"
        };
        await userManager.CreateAsync(studentUser, "StudentPassword@123");
    }
    // Ensure the user is in the Student role
    if (!await userManager.IsInRoleAsync(studentUser, "Student"))
    {
        await userManager.AddToRoleAsync(studentUser, "Student");
    }

    // Seed Supervisor User
    var supervisorEmail = "supervisor1@test.com";
    var supervisorUser = await userManager.FindByEmailAsync(supervisorEmail);
    if (supervisorUser == null)
    {
        supervisorUser = new ApplicationUser
        {
            UserName = supervisorEmail,
            Email = supervisorEmail,
            EmailConfirmed = true,
            FullName = "Test Supervisor",
            UserRole = "Supervisor",
            Expertise = "Artificial Intelligence, Machine Learning, IoT, Python, TensorFlow"
        };
        await userManager.CreateAsync(supervisorUser, "SupervisorPassword@123");
    }
    // Ensure the user is in the Supervisor role
    if (!await userManager.IsInRoleAsync(supervisorUser, "Supervisor"))
    {
        await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
    }
}

// 5. Middleware Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Standard security setting for production
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication ensures the app knows WHO you are
app.UseAuthentication();
// Authorization ensures the app knows what you are ALLOWED to do
app.UseAuthorization();

// 6. Default Route: Directs the app to HomeController/Index by default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // This is required for Identity pages to work

app.Run();