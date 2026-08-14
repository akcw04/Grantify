using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Grantify.Areas.Identity.Pages.Account;

// SHARED PAGE. Our own sign-up page, replacing the one that ships with ASP.NET
// Core Identity.
//
// Why we replaced it: the built-in page creates the account and nothing else, so
// the new user had NO ROLE. Every area of this site is behind a role policy
// (Program.cs), which meant a student who signed up could not open a single
// page — they were sent to "Access denied" and there was no way out, because the
// Admin screen can only grant the Officer role. Signing up looked like it
// worked and produced a dead account.
//
// This page puts the new account in the Student role, which is what the sign-in
// page promises ("New student? Create an account"). Officer and administrator
// accounts are still made by an administrator, never here.
[AllowAnonymous]
public class RegisterModel : PageModel
{
    // The only role a person may give themselves. Officer and Admin are handed
    // out by an administrator.
    private const string SelfServiceRole = "Student";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(200, MinimumLength = 3)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Length matches the policy in Program.cs, so the browser refuses a
        // too-short password before the server has to.
        [Required(ErrorMessage = "Please choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // "~/" is the home page, which sends each role on to its own dashboard.
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName.Trim(),
            // Program.cs sets RequireConfirmedAccount = false and we have no
            // email service, so there is nothing to confirm.
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            // Identity's reasons are already written for the person reading them
            // ("Passwords must have at least one digit", "Email is already taken").
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        // The important line. Without a role the account cannot open any page on
        // this site, and no other screen can put that right.
        await _userManager.AddToRoleAsync(user, SelfServiceRole);

        _logger.LogInformation("New {Role} account created for {Email}.", SelfServiceRole, Input.Email);

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }
}
