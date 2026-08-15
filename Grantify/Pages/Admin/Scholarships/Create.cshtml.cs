using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grantify.Pages.Admin.Scholarships;

public class CreateModel : AdminPageModel
{
    private readonly ScholarshipAdminService _scholarships;

    public CreateModel(ScholarshipAdminService scholarships)
    {
        _scholarships = scholarships;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> InstitutionOptions { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> IntakePeriodOptions { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter a scholarship name.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the provider.")]
        [StringLength(200)]
        public string Provider { get; set; } = string.Empty;

        // string? on purpose. With <Nullable>enable</Nullable> a non-nullable
        // string is treated as [Required] automatically, which silently made
        // this optional field mandatory. Nullable keeps it genuinely optional.
        [StringLength(2000)]
        public string? Description { get; set; }

        // NULLABLE on purpose - see Pages/Student/Profile.cshtml.cs. A plain
        // decimal renders value="0", hiding the placeholder and forcing the
        // admin to delete the 0 before typing. [Required] still enforces entry.
        [Required(ErrorMessage = "Please enter the minimum CGPA.")]
        [Range(0, 4.00, ErrorMessage = "CGPA must be between 0.00 and 4.00.")]
        [Display(Name = "Minimum CGPA")]
        public decimal? MinimumCgpa { get; set; }

        [Range(0, 1000000, ErrorMessage = "Household income cannot be negative.")]
        [Display(Name = "Maximum household income (RM)")]
        public decimal? MaximumHouseholdIncome { get; set; }

        [Required(ErrorMessage = "Please enter a deadline.")]
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Published")]
        public bool IsPublished { get; set; }

        [Display(Name = "Institution")]
        public int? InstitutionId { get; set; }

        [Display(Name = "Eligible study category")]
        public int? ScholarshipCategoryId { get; set; }

        [Display(Name = "Intake period")]
        public int? IntakePeriodId { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _scholarships.CreateAsync(new Scholarship
        {
            Name = Input.Name,
            Provider = Input.Provider,
            Description = Input.Description ?? string.Empty,
            MinimumCgpa = Input.MinimumCgpa!.Value,
            MaximumHouseholdIncome = Input.MaximumHouseholdIncome,
            Deadline = Input.Deadline,
            IsPublished = Input.IsPublished,
            InstitutionId = Input.InstitutionId,
            ScholarshipCategoryId = Input.ScholarshipCategoryId,
            IntakePeriodId = Input.IntakePeriodId
        });

        SetMessage($"Scholarship \"{Input.Name}\" was created.");
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var institutions = await _scholarships.GetInstitutionOptionsAsync();
        InstitutionOptions = institutions
            .Select(i => new SelectListItem(i.Name, i.Id.ToString()))
            .ToList();

        var categories = await _scholarships.GetCategoryOptionsAsync();
        CategoryOptions = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();

        var intakePeriods = await _scholarships.GetIntakePeriodOptionsAsync();
        IntakePeriodOptions = intakePeriods
            .Select(p => new SelectListItem(p.PeriodName, p.Id.ToString()))
            .ToList();
    }
}
