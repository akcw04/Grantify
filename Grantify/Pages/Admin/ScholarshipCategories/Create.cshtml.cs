using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.ScholarshipCategories;

public class CreateModel : AdminPageModel
{
    private readonly ScholarshipCategoryService _categories;

    public CreateModel(ScholarshipCategoryService categories)
    {
        _categories = categories;
    }

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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _categories.CreateAsync(new ScholarshipCategory
        {
            Name = Input.Name,
            Description = Input.Description
        });

        SetMessage($"Category \"{Input.Name}\" was created.");
        return RedirectToPage("Index");
    }
}
