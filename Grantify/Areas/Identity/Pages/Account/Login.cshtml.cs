using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Areas.Identity.Pages.Account;

// SHARED PAGE. Our own sign-in page, replacing the one that ships with ASP.NET
// Core Identity.
//
// Why we replaced it: the built-in page offers "Forgot your password?" and
// "Resend email confirmation". Both need an email service, and we have none —
// so they led to pages that looked like they worked but sent nothing. A dead
// link is worse than a missing one, especially to somebody marking the system.
//
// We do not need email confirmation at all: Program.cs sets
// RequireConfirmedAccount = false, and seeded accounts are created already
// confirmed. Accounts are created by an administrator, not by self sign-up.
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        // Clear any half-finished sign-in left over from a previous attempt.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Where to send them afterwards. "~/" is the home page, which sends each
        // role on to its own dashboard.
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // lockoutOnFailure: true means repeated wrong passwords lock the account
        // for a while, which is what stops somebody guessing their way in.
        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in.", Input.Email);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account {Email} is locked out.", Input.Email);
            ModelState.AddModelError(string.Empty,
                "This account is temporarily locked after too many failed attempts. Please try again shortly.");
            return Page();
        }

        // Deliberately vague: saying "no such account" would tell a stranger
        // which email addresses exist on the system.
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }
}
