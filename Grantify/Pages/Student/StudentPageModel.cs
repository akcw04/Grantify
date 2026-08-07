using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Student;

// OWNER: Member A.
// Small base class every student page inherits, so each page does not repeat
// "who is logged in" and the green/red message strip.
public abstract class StudentPageModel : PageModel
{
    // The signed-in student's user id. Pages/Student/ is behind the
    // "StudentOnly" policy (Program.cs), so there is always a user here.
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Shown once after a redirect, then cleared. TempData survives the redirect;
    // a normal property would not.
    [Microsoft.AspNetCore.Mvc.TempData]
    public string? StatusMessage { get; set; }

    [Microsoft.AspNetCore.Mvc.TempData]
    public bool StatusIsError { get; set; }

    protected void SetMessage(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}
