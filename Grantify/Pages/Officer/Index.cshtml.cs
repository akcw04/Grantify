using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Officer;

// Landing page of the Officer area.
// Access rule: only the "Officer" role can open pages in this folder (set in Program.cs).
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // Nothing to load yet. Member B: replace this page with real features.
    }
}
