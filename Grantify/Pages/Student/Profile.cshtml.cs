using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Student;

// #1 - the student keeps their academic and financial details here.
// These are the numbers the eligibility check reads, so nothing on the
// Scholarships page works properly until this form is filled in.
public class ProfileModel : StudentPageModel
{
    private readonly StudentService _students;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(StudentService students, UserManager<ApplicationUser> userManager)
    {
        _students = students;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsComplete { get; private set; }

    // The fields of the form. Validation attributes give client-side messages
    // for free and are checked again on the server by ModelState.IsValid.
    public class InputModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(200, MinimumLength = 3)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Range(0, 4.00, ErrorMessage = "CGPA must be between 0.00 and 4.00.")]
        [Display(Name = "Current CGPA")]
        public decimal Cgpa { get; set; }

        [Range(0, 1000000, ErrorMessage = "Household income cannot be negative.")]
        [Display(Name = "Monthly household income (RM)")]
        public decimal HouseholdIncome { get; set; }

        [Required(ErrorMessage = "Please enter your course.")]
        [StringLength(200)]
        [Display(Name = "Course")]
        public string Course { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your institution.")]
        [StringLength(200)]
        [Display(Name = "Institution")]
        public string Institution { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var profile = await _students.GetOrCreateProfileAsync(CurrentUserId);
        var user = await _userManager.GetUserAsync(User);

        Input = new InputModel
        {
            FullName = user?.FullName ?? string.Empty,
            Cgpa = profile.Cgpa,
            HouseholdIncome = profile.HouseholdIncome,
            Course = profile.Course,
            Institution = profile.Institution
        };

        IsComplete = StudentService.IsProfileComplete(profile);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            IsComplete = false;
            return Page();
        }

        // The name is on the login account, the rest is on the profile.
        var user = await _userManager.GetUserAsync(User);
        if (user is not null && user.FullName != Input.FullName)
        {
            user.FullName = Input.FullName;
            await _userManager.UpdateAsync(user);
        }

        await _students.SaveProfileAsync(
            CurrentUserId, Input.Cgpa, Input.HouseholdIncome, Input.Course, Input.Institution);

        SetMessage("Profile saved. Your eligibility results have been updated.");
        return RedirectToPage();
    }
}
