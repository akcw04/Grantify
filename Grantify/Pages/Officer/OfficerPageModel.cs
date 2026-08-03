using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Officer;

// OWNER: Member B (Officer role).
//
// Shared base class for every Officer page. It handles the two things all of
// them need, so no page has to repeat the code:
//   1. Working out WHO the logged-in officer is (id + name), which every
//      OfficerService method needs for the audit trail.
//   2. Carrying a green "it worked" / red "it failed" message across a redirect.
//
// A new Officer page should inherit from this instead of PageModel.
public abstract class OfficerPageModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    protected OfficerPageModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // TempData survives exactly one redirect, which is what we want:
    // POST -> save -> redirect back to the page -> show the message once.
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    // Copies the outcome of a service call into the message bar.
    protected void ShowResult(ServiceResult result)
    {
        StatusMessage = result.Message;
        StatusIsError = !result.Success;
    }

    // The officer doing the work. We pass both values to OfficerService so the
    // history can say "Approved by Test Officer" without an extra join later.
    protected async Task<(string UserId, string Name)> GetOfficerAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        // Should never be null: the folder rule in Program.cs already blocks
        // anyone who is not a logged-in Officer from reaching these pages.
        if (user is null)
        {
            return (string.Empty, "Unknown officer");
        }

        var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Officer" : user.FullName;
        return (user.Id, name);
    }
}
