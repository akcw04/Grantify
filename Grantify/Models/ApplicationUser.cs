using Microsoft.AspNetCore.Identity;

namespace Grantify.Models;

// Our own user class. It is the normal Identity login user plus extra fields we need.
// Every person who logs in (Student, Officer, Admin) is one row of this.
public class ApplicationUser : IdentityUser
{
    // The person's full name, shown in the navbar and on review pages.
    public string FullName { get; set; } = string.Empty;
}
