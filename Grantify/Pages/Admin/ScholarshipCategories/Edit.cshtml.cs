using System.ComponentModel.DataAnnotations;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.ScholarshipCategories;

public class EditModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public EditModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter a category name.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _categories.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        Id = category.Id;
        Input = new InputModel
        {
            Name = category.Name,
            Description = category.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var updated = await _categories.UpdateAsync(Id, Input.Name, Input.Description);
        if (!updated)
        {
            return NotFound();
        }

        SetMessage($"Category \"{Input.Name}\" was updated.");
        return RedirectToPage("Index");
    }
}
