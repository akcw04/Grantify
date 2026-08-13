using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Admin;

// OWNER: Member C. Small base class every admin CRUD page inherits, so each
// page does not repeat the green/red status message strip (same pattern as
// Pages/Student/StudentPageModel.cs and Pages/Officer/OfficerPageModel.cs).
public abstract class AdminPageModel : PageModel
{
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
