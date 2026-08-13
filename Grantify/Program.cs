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

// How often a signed-in browser's cookie is re-checked against the database.
// The default is 30 minutes; a deactivated user's existing session would stay
// signed in for up to that long otherwise. One minute keeps Admin's "deactivate"
// button effective almost immediately without checking the database every request.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

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

// Decides where uploaded documents are kept: Amazon S3 when the environment
// property Storage__BucketName is set (the deployed site), otherwise a folder on
// this machine (our laptops). Used by both the Student and Officer modules.
builder.Services.AddScoped<DocumentStorageService>();

// Sends the award / rejection email through Amazon SNS when the environment
// property Notifications__TopicArn is set. Without it, nothing is sent and a
// line goes in the log — so local development needs no AWS setup.
builder.Services.AddScoped<DecisionNotifier>();
builder.Services.AddScoped<OfficerService>();   // Member B — Officer role
builder.Services.AddScoped<StudentService>();   // Member A — Student role

// Master data and account management (Member C — Admin role).
builder.Services.AddScoped<ScholarshipCategoryService>();
builder.Services.AddScoped<InstitutionService>();
builder.Services.AddScoped<IntakePeriodService>();
builder.Services.AddScoped<AdminUserService>();

var app = builder.Build();

// ---------- 5. Prepare the database on startup ----------
// Applies any new migrations, then fills in the starting data.
// This is why "F5 after git pull" is usually all you need, and why the tables
// build themselves on RDS the first time we deploy.
//
// The test accounts and sample scholarships are DEVELOPMENT ONLY — their
// passwords are printed in README.md, so they must never exist on the deployed
// site. See DbSeeder for what the deployed site gets instead.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider, app.Environment.IsDevelopment());

        // TEMPORARY (Member B): fake applications so the Officer review queue is not
        // empty while the Student pages are still being built. Development only.
        // Delete this block, and OfficerDemoSeeder.cs, once Member A's pages work.
        if (app.Environment.IsDevelopment())
        {
            await OfficerDemoSeeder.SeedAsync(scope.ServiceProvider);
        }
    }
    catch (Exception ex)
    {
        // The database was unreachable, or the migrations failed.
        //
        // On the DEPLOYED site we log it and let the web server start anyway.
        // Crashing here would be much worse than it sounds: Elastic Beanstalk
        // reads "the app exited" as a failed deployment and rolls the
        // configuration back — including the very connection string we were
        // trying to fix. That leaves you unable to correct the setting,
        // because correcting it restarts an app that dies. A running site
        // showing an error page is far easier to diagnose than a dead one.
        //
        // On OUR machines we still crash on purpose: if F5 cannot reach
        // LocalDB, you want to know immediately, not chase a broken page.
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");

        logger.LogError(ex,
            "Database setup failed on startup. The site will start, but pages that " +
            "read data will not work until the database is reachable. Check the " +
            "ConnectionStrings__DefaultConnection setting and that the database " +
            "security group allows this server on port 1433.");

        if (app.Environment.IsDevelopment())
        {
            throw;
        }
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
