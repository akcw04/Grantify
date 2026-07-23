using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Admin;

// Landing page of the Admin area.
// Access rule: only the "Admin" role can open pages in this folder (set in Program.cs).
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // Nothing to load yet. Member C: replace this page with real features.
    }
}
