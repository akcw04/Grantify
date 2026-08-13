using System.Security.Claims;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Admin;

// OWNER: Member C (Admin role).
// Small base class every admin page inherits, so each page does not repeat
// "who is logged in" and the green/red message strip (same pattern as
// Pages/Student/StudentPageModel.cs and Pages/Officer/OfficerPageModel.cs).
public abstract class AdminPageModel : PageModel
{
    // The signed-in administrator's user id. Pages/Admin/ is behind the
    // "AdminOnly" policy (Program.cs), so there is always a user here.
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Shown once after a redirect, then cleared. TempData survives the redirect;
    // a normal property would not.
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    protected void SetMessage(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    // Copies the outcome of a service call into the message bar.
    protected void ShowResult(ServiceResult result)
    {
        StatusMessage = result.Message;
        StatusIsError = !result.Success;
    }
}
