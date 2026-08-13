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

    // True when the account being edited is an administrator, and when it is
    // the administrator doing the editing. The form uses both to explain why
    // the "Active" switch is not always available.
    public bool IsAdminAccount { get; private set; }
    public bool IsOwnAccount => Id == CurrentUserId;

    public class InputModel
    {
        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Scholarship officer")]
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
        IsAdminAccount = user.IsAdmin;
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

        // The "Active" switch is disabled on your own account, and a disabled
        // checkbox posts nothing at all — which arrives here as "off" and would
        // deactivate the very account being used. Read it back as it really is.
        var isActive = IsOwnAccount || Input.IsActive;

        // The service refuses a change that would lock every administrator out
        // of the system. When it does, stay on the form and say why instead of
        // redirecting away as if the save had worked.
        var result = await _users.UpdateUserAsync(Id, isActive, Input.IsOfficer, CurrentUserId);

        if (!result.Success)
        {
            Email = existing.Email;
            FullName = existing.FullName;
            IsAdminAccount = existing.IsAdmin;
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        ShowResult(result);
        return RedirectToPage("Index");
    }
}
