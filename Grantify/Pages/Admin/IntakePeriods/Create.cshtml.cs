using System.ComponentModel.DataAnnotations;
using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Admin.IntakePeriods;

public class CreateModel : AdminPageModel
{
    private readonly IntakePeriodService _intakePeriods;

    public CreateModel(IntakePeriodService intakePeriods)
    {
        _intakePeriods = intakePeriods;
    }

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
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please enter an end date.")]
        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; } = DateTime.Today;
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

        await _intakePeriods.CreateAsync(new IntakePeriod
        {
            PeriodName = Input.PeriodName,
            StartDate = Input.StartDate,
            EndDate = Input.EndDate
        });

        SetMessage($"Intake period \"{Input.PeriodName}\" was created.");
        return RedirectToPage("Index");
    }
}
