using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String: Retrieves the database path from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register DbContext: Links your ApplicationDbContext to SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

// 3. Identity Setup: Uses the built-in IdentityUser for authentication with Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI(); // Add this line!

// Add Authorization Services with Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("SupervisorOnly", policy => policy.RequireRole("Supervisor"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// 4. Add MVC Services: Enables Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Data: Automatically populate roles, research areas, and admin user on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    dbContext.Database.Migrate(); // Temporarily disabled to test startup

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
    if (!dbContext.ResearchAreas.Any())
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
        dbContext.ResearchAreas.AddRange(researchAreas);
        await dbContext.SaveChangesAsync();
    }

    // Seed Default Admin User
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