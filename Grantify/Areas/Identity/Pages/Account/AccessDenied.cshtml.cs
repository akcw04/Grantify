using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Areas.Identity.Pages.Account;

// Backing model for our own Access denied page. There is nothing to load —
// the page only needs to know who is logged in, which it reads from User.
public class AccessDeniedModel : PageModel
{
    public void OnGet()
    {
    }
}
