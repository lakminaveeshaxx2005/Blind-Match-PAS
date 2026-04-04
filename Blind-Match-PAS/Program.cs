using Microsoft.EntityFrameworkCore;
using Blind_Match_PAS.Data;
using Blind_Match_PAS.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Retrieve the Connection String from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register the ApplicationDbContext with SQL Server support
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Add Identity services for authentication and link it to the DbContext
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4. Add MVC services (Controllers with Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// 6. Define the default route for the application
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();