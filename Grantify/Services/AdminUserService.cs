using Grantify.Models;
using Microsoft.AspNetCore.Identity;

namespace Grantify.Services;

public class AdminUserService
{
    public const string OfficerRole = "Officer";

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
            var isActive = !await _userManager.IsLockedOutAsync(user);

            items.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsActive = isActive,
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
            IsOfficer = await _userManager.IsInRoleAsync(user, OfficerRole)
        };
    }

    public async Task<bool> UpdateUserAsync(string id, bool isActive, bool isOfficer)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        await _userManager.SetLockoutEndDateAsync(user, isActive ? null : DateTimeOffset.MaxValue);

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

        return true;
    }
}

public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserEditDetails
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsOfficer { get; set; }
}
