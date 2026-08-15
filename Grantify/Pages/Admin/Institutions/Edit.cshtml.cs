using System.ComponentModel.DataAnnotations;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Institutions;

public class EditModel : AdminPageModel
{
    private readonly InstitutionService _institutions;

    public EditModel(InstitutionService institutions)
    {
        _institutions = institutions;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter an institution name.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // string? on purpose. With <Nullable>enable</Nullable> a non-nullable
        // string is treated as [Required] automatically, which silently made
        // this optional field mandatory. Nullable keeps it genuinely optional.
        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(256)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Contact email")]
        public string? ContactEmail { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var institution = await _institutions.GetByIdAsync(id);
        if (institution is null)
        {
            return NotFound();
        }

        Id = institution.Id;
        Input = new InputModel
        {
            Name = institution.Name,
            Location = institution.Location,
            ContactEmail = institution.ContactEmail
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var updated = await _institutions.UpdateAsync(
            Id, Input.Name, Input.Location ?? string.Empty, Input.ContactEmail ?? string.Empty);
        if (!updated)
        {
            return NotFound();
        }

        SetMessage($"Institution \"{Input.Name}\" was updated.");
        return RedirectToPage("Index");
    }
}
