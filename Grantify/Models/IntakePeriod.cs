using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// A named application window (e.g. "Fall 2026 Intake") that scholarships can be tied to.
public class IntakePeriod
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string PeriodName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
