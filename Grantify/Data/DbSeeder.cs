using Grantify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Data;

// Fills an empty database with starting data so everyone on the team
// sees the same roles, test accounts and sample scholarships.
// Runs automatically every time the app starts (see Program.cs).
// It only adds things that are missing — it never doubles the data.
public static class DbSeeder
{
    // isDevelopment decides how much gets seeded.
    //
    // On our own machines we want the three test accounts and the sample
    // scholarships, so the app is usable the moment you press F5.
    //
    // On the DEPLOYED site we must NOT create them. The passwords are written in
    // README.md, so seeding admin@grantify.test on a public address would hand
    // anyone who reads the repo a full administrator login. In that case we only
    // create the roles, plus one administrator whose details come from
    // configuration (an Elastic Beanstalk environment property), and nothing at all
    // if those are not supplied.
    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment)
    {
        var db = services.GetRequiredService<AppDbContext>();

        // First, bring the database up to date with the latest migrations.
        // This means teammates do not need to run Update-Database by hand,
        // and the tables are created on RDS the first time we deploy.
        await db.Database.MigrateAsync();

        // Roles are part of how the app works, not test data — always needed.
        await CreateRolesAsync(services);

        if (isDevelopment)
        {
            await CreateTestAccountsAsync(services, db);

            // Demo scholarships and applications, so every dashboard has
            // something to show the moment you press F5.
            await DemoDataSeeder.SeedAsync(services, "Grantify@123");
        }
        else
        {
            await CreateConfiguredAccountsAsync(services);

            // The DEPLOYED site only gets the demonstration data when asked.
            // Two environment properties are needed, and the password must
            // come from configuration — never from this repository, because
            // these accounts can sign in to the public site.
            var config = services.GetRequiredService<IConfiguration>();
            if (string.Equals(config["Seed:DemoData"], "true", StringComparison.OrdinalIgnoreCase))
            {
                var demoPassword = config["Seed:DemoPassword"];
                if (string.IsNullOrWhiteSpace(demoPassword))
                {
                    services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder").LogWarning(
                        "Seed__DemoData is true but Seed__DemoPassword is not set, " +
                        "so no demonstration data was seeded.");
                }
                else
                {
                    await DemoDataSeeder.SeedAsync(services, demoPassword);
                }
            }
        }
    }

    // Accounts for the DEPLOYED site, taken from configuration so no password is
    // ever written in the source code. In Elastic Beanstalk these are set as
    // environment properties:
    //
    //   Seed__AdminEmail    / Seed__AdminPassword      (required)
    //   Seed__OfficerEmail  / Seed__OfficerPassword    (optional)
    //   Seed__StudentEmail  / Seed__StudentPassword    (optional)
    //
    // Why the officer and student ones exist: a person who registers on the site
    // gets no role, and the page that would give them one is the Admin user
    // management screen. Until that exists, there would be no way for the team to
    // demonstrate the Officer and Student features on the deployed site. These
    // three logins bootstrap the demonstration.
    //
    // Anything left unset is simply skipped, and a note appears in the log.
    private static async Task CreateConfiguredAccountsAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await CreateConfiguredUserAsync(services, config, logger, "Admin", "Administrator");
        await CreateConfiguredUserAsync(services, config, logger, "Officer", "Scholarship Officer");
        await CreateConfiguredUserAsync(services, config, logger, "Student", "Student");
    }

    // Creates one account for one role, if its two settings are present.
    private static async Task CreateConfiguredUserAsync(
        IServiceProvider services,
        IConfiguration config,
        ILogger logger,
        string role,
        string fullName)
    {
        var email = config[$"Seed:{role}Email"];
        var password = config[$"Seed:{role}Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No {Role} account was seeded. Set Seed__{Role}Email and Seed__{Role}Password " +
                "as environment properties if you need one.", role, role, role);
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // The account may already exist from an earlier start. If so we RESET its
        // password to whatever the configuration now says, rather than leaving it
        // alone.
        //
        // Why: these three accounts are a deployment setting, not people's
        // personal logins. Skipping them used to mean that changing
        // Seed__AdminPassword did nothing at all — the site kept the password
        // from whenever the account was first created, with no error and no clue
        // why the new one was refused. Making configuration the source of truth
        // means a wrong password fixes itself on the next restart.
        //
        // The cost is that these accounts cannot keep a password changed inside
        // the app; it goes back to the configured one when the site restarts.
        // For bootstrap accounts that is the behaviour we want.
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
            var reset = await userManager.ResetPasswordAsync(existing, resetToken, password);

            if (!reset.Succeeded)
            {
                logger.LogError("Could not update the {Role} account password: {Errors}",
                    role, string.Join("; ", reset.Errors.Select(e => e.Description)));
                return;
            }

            // The role may be missing if the account was made some other way.
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            logger.LogInformation("Updated the existing {Role} account from configuration.", role);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // Never log the password. The reasons say things like "too short" or
            // "must have an uppercase letter".
            logger.LogError("Could not create the {Role} account: {Errors}",
                role, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created the {Role} account from configuration.", role);
    }

    // The three roles of our system. A user's role decides which pages they can open.
    private static async Task CreateRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { "Student", "Officer", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Test accounts for development only. NEVER use these in the real deployed app.
    // Password for all of them: Grantify@123
    private static async Task CreateTestAccountsAsync(IServiceProvider services, AppDbContext db)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var student = await CreateUserAsync(userManager, "student@grantify.test", "Test Student", "Student");
        await CreateUserAsync(userManager, "officer@grantify.test", "Test Officer", "Officer");
        await CreateUserAsync(userManager, "admin@grantify.test", "Test Admin", "Admin");

        // Give the test student a profile so the eligibility check has numbers to work with.
        if (student is not null && !await db.StudentProfiles.AnyAsync(p => p.UserId == student.Id))
        {
            db.StudentProfiles.Add(new StudentProfile
            {
                UserId = student.Id,
                Cgpa = 3.50m,
                HouseholdIncome = 4000m,
                Course = "Software Engineering",
                Institution = "Asia Pacific University"
            });
            await db.SaveChangesAsync();
        }
    }

    // Makes one user with the given role, or returns the existing one if already made.
    private static async Task<ApplicationUser?> CreateUserAsync(
        UserManager<ApplicationUser> userManager, string email, string fullName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Grantify@123");
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // Sample scholarships used to be created here; they now live in
    // DemoDataSeeder together with the demo students and applications, so the
    // deployed site can opt in to the same data set.
}
