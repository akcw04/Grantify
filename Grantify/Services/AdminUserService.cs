using Grantify.Models;
using Microsoft.AspNetCore.Identity;

namespace Grantify.Services;

// OWNER: Member C (Admin role).
//
// Backend logic for the account management screens, so the pages stay thin.
// Every rule about "what an administrator is allowed to do to an account" lives
// in this one class.
//
// How "deactivate" works: Identity has no on/off switch, so we use its lockout
// instead. Setting LockoutEnd far into the future refuses the sign-in, and
// clearing it lets the person back in. Program.cs re-checks a signed-in cookie
// against the database every minute, so switching an account off takes effect
// almost immediately instead of when the cookie happens to expire.
//
// Registered in Program.cs: builder.Services.AddScoped<AdminUserService>();
public class AdminUserService
{
    public const string OfficerRole = "Officer";
    public const string AdminRole = "Admin";

    // Far enough ahead to mean "off", and far enough from Identity's own
    // failed-attempt lockout that the sign-in page can tell the two apart.
    private static readonly DateTimeOffset DeactivatedUntil = DateTimeOffset.MaxValue;

    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<UserListItem>> GetAllUsersAsync()
    {
        var users = _userManager.Users
            .OrderBy(u => u.Email)
            .ToList();

        var items = new List<UserListItem>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            items.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsActive = !await _userManager.IsLockedOutAsync(user),
                Roles = roles.OrderBy(r => r).ToList()
            });
        }

        return items;
    }

    public async Task<UserEditDetails?> GetUserForEditAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        return new UserEditDetails
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsActive = !await _userManager.IsLockedOutAsync(user),
            IsOfficer = await _userManager.IsInRoleAsync(user, OfficerRole),
            IsAdmin = await _userManager.IsInRoleAsync(user, AdminRole)
        };
    }

    // Saves the changes an administrator made to one account.
    //
    // currentAdminId is the administrator doing the editing. We need it for the
    // two guards below, both of which exist to stop an administrator locking
    // the system's own door from the inside.
    public async Task<ServiceResult> UpdateUserAsync(string id, bool isActive, bool isOfficer, string currentAdminId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail("That account no longer exists.");
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, AdminRole);

        // Guard 1: you cannot switch off the account you are signed in with.
        // The cookie is re-checked every minute, so this used to sign the
        // administrator out moments later with no way back in.
        if (!isActive && user.Id == currentAdminId)
        {
            return ServiceResult.Fail("You cannot deactivate the account you are signed in with.");
        }

        // Guard 2: somebody must always be able to administer the system. If
        // this is the last active administrator, switching it off would leave
        // nobody able to reach the Admin pages at all.
        if (!isActive && isAdmin && await CountActiveAdminsAsync() <= 1)
        {
            return ServiceResult.Fail(
                "This is the only active administrator. Give another account the Admin role before deactivating it.");
        }

        await _userManager.SetLockoutEndDateAsync(user, isActive ? null : DeactivatedUntil);

        // Changing the security stamp invalidates any browser already signed in
        // as this person, so a deactivated user is turned away on their next
        // request rather than carrying on until their cookie expires.
        if (!isActive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var currentlyOfficer = await _userManager.IsInRoleAsync(user, OfficerRole);
        if (isOfficer && !currentlyOfficer)
        {
            await _userManager.AddToRoleAsync(user, OfficerRole);
        }
        else if (!isOfficer && currentlyOfficer)
        {
            await _userManager.RemoveFromRoleAsync(user, OfficerRole);
        }

        return ServiceResult.Ok($"{user.Email} was updated.");
    }

    // How many administrators can still sign in. Used by the guard above.
    private async Task<int> CountActiveAdminsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync(AdminRole);

        var active = 0;
        foreach (var admin in admins)
        {
            if (!await _userManager.IsLockedOutAsync(admin))
            {
                active++;
            }
        }

        return active;
    }
}

// ----------------------------------------------------------------------
// Small classes used only by the admin account screens.
// ----------------------------------------------------------------------

// One row of the user table.
public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

// Everything the edit form needs for one account.
public class UserEditDetails
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsOfficer { get; set; }

    // Shown as a warning on the edit form, so an administrator knows they are
    // editing another administrator before they change anything.
    public bool IsAdmin { get; set; }
}
