using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Institutions;

public class CreateModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public CreateModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter an institution name.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(256)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Contact email")]
        public string ContactEmail { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _institutions.CreateAsync(new Institution
        {
            Name = Input.Name,
            Location = Input.Location,
            ContactEmail = Input.ContactEmail
        });

        SetMessage($"Institution \"{Input.Name}\" was created.");
        return RedirectToPage("Index");
    }
}
