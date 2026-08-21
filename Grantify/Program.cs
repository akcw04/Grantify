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
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // The password rules, written out instead of inherited. Identity's
        // defaults were on anyway, but nothing on the register page said so —
        // people typed a simple password, were refused, and read it as
        // "registration is broken". The register page now lists these same
        // rules under the password box, so keep the two in step.
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
    })
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

// Puts an uploaded document on the Amazon SQS queue that feeds the processing
// microservice, when the environment property Documents__ProcessingQueueUrl is
// set. Without it nothing is queued and the document stays Pending for an
// officer to check by hand — exactly the Task 1 behaviour.
builder.Services.AddScoped<DocumentQueuePublisher>();

// Sends the award / rejection email through Amazon SNS when the environment
// property Notifications__TopicArn is set. Without it, nothing is sent and a
// line goes in the log — so local development needs no AWS setup.
builder.Services.AddScoped<DecisionNotifier>();

// ---------- 4b. Caching (Amazon ElastiCache for Redis in the cloud) ----------
// The public landing page is the one page every anonymous visitor hits, and it
// runs three database queries per view. Its results are cached for a minute:
//
//   Cache__RedisEndpoint set  -> Amazon ElastiCache (Redis), shared by every
//                                web server, survives an app restart
//   not set                   -> the same cache kept in this process's memory
//
// Same pattern as S3 and SNS: one environment property switches the cloud
// service on, and F5 on a laptop needs nothing. abortConnect=false means the
// app still starts when Redis is briefly unreachable, and the cache reads
// themselves are wrapped in try/catch so a Redis outage slows nothing down —
// the page just falls back to the database.
var redisEndpoint = builder.Configuration["Cache:RedisEndpoint"];
if (!string.IsNullOrWhiteSpace(redisEndpoint))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = $"{redisEndpoint.Trim()},abortConnect=false,connectTimeout=3000";
        options.InstanceName = "grantify:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// ---------- 4c. Eligibility microservice (AWS Lambda + API Gateway) ----------
// When Eligibility__ApiUrl is set, eligibility checks are POSTed to the
// screening microservice (a Lambda behind API Gateway) instead of running
// in-process. The 3-second timeout matters: if the remote service is slow or
// down, EligibilityService falls back to its local copy of the rules, so a
// student can always browse. See EligibilityService.CheckAsync.
builder.Services.AddHttpClient("eligibility", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<OfficerService>();   // Member B — Officer role
builder.Services.AddScoped<StudentService>();   // Member A — Student role

// Master data and account management (Member C — Admin role).
builder.Services.AddScoped<ScholarshipCategoryService>();
builder.Services.AddScoped<InstitutionService>();
builder.Services.AddScoped<IntakePeriodService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<ScholarshipAdminService>();
builder.Services.AddScoped<AnalyticsService>();

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
        // DbSeeder also decides whether the demonstration data is seeded:
        // always on our own machines, and on the deployed site only when the
        // Seed__DemoData environment property says so. See DemoDataSeeder.
        await DbSeeder.SeedAsync(scope.ServiceProvider, app.Environment.IsDevelopment());
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
