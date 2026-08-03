using Grantify.Data;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Database ----------
// The connection string lives in appsettings.json.
// During development it points to LocalDB on YOUR machine.
// In Week 9 we only change that one string to point to AWS RDS — no code changes.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------- 2. Login system (ASP.NET Core Identity) ----------
// RequireConfirmedAccount is off because we do not send real confirmation
// emails during development.
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------- 3. Who can open which pages ----------
// Each folder under Pages/ belongs to one role.
// Example: only logged-in users with the "Student" role can open Pages/Student/*.
// You do NOT need [Authorize] on every page — the folder rule below covers it.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("OfficerOnly", policy => policy.RequireRole("Officer"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Student", "StudentOnly");
    options.Conventions.AuthorizeFolder("/Officer", "OfficerOnly");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

// ---------- 4. Backend services ----------
// Register every class from the Services/ folder here (one line each),
// then any page can ask for it in its constructor.
builder.Services.AddScoped<ScholarshipService>();
builder.Services.AddScoped<EligibilityService>();
builder.Services.AddScoped<OfficerService>();   // Member B — Officer role

var app = builder.Build();

// ---------- 5. Prepare the database on startup ----------
// Applies any new migrations and fills in roles, test accounts and sample data.
// This is why "F5 after git pull" is usually all you need.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);

    // TEMPORARY (Member B): fake applications so the Officer review queue is not
    // empty while the Student pages are still being built. Development only.
    // Delete this block, and OfficerDemoSeeder.cs, once Member A's pages work.
    if (app.Environment.IsDevelopment())
    {
        await OfficerDemoSeeder.SeedAsync(scope.ServiceProvider);
    }
}

// ---------- 6. Request pipeline (mostly template defaults) ----------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
