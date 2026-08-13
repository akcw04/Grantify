using System.ComponentModel.DataAnnotations;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.Users;

public class EditModel : AdminPageModel
{
    private readonly AdminUserService _users;

    public EditModel(AdminUserService users)
    {
        _users = users;
    }

    [BindProperty]
    public string Id { get; set; } = string.Empty;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public class InputModel
    {
        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Scholarship Officer")]
        public bool IsOfficer { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _users.GetUserForEditAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        Id = user.Id;
        Email = user.Email;
        FullName = user.FullName;
        Input = new InputModel
        {
            IsActive = user.IsActive,
            IsOfficer = user.IsOfficer
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _users.GetUserForEditAsync(Id);
        if (existing is null)
        {
            return NotFound();
        }

        await _users.UpdateUserAsync(Id, Input.IsActive, Input.IsOfficer);

        SetMessage($"{existing.Email} was updated.");
        return RedirectToPage("Index");
    }
}
