using System.ComponentModel.DataAnnotations;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.IntakePeriods;

public class EditModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public EditModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please enter a period name.")]
        [StringLength(200)]
        [Display(Name = "Period name")]
        public string PeriodName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Please enter an end date.")]
        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var period = await _intakePeriods.GetByIdAsync(id);
        if (period is null)
        {
            return NotFound();
        }

        Id = period.Id;
        Input = new InputModel
        {
            PeriodName = period.PeriodName,
            StartDate = period.StartDate,
            EndDate = period.EndDate
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.EndDate < Input.StartDate)
        {
            ModelState.AddModelError(nameof(Input.EndDate), "End date cannot be before the start date.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var updated = await _intakePeriods.UpdateAsync(Id, Input.PeriodName, Input.StartDate, Input.EndDate);
        if (!updated)
        {
            return NotFound();
        }

        SetMessage($"Intake period \"{Input.PeriodName}\" was updated.");
        return RedirectToPage("Index");
    }
}
