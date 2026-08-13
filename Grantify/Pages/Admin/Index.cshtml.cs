using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Pages.Admin;

// OWNER: Member C (Admin role).
//
// The root of the Admin area. There is no separate landing page: the Analytics
// dashboard IS the administrator's home, and it is where the navbar and the
// sign-in redirect already send them. This page only exists so that /Admin
// takes somebody who types it to the same place instead of a dead end.
public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/Analytics/Index");
}
