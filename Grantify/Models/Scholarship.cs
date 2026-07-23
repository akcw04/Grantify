using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Models;

// One scholarship that students can apply for.
// Admins create these; students browse them.
// NOTE: if you need more fields, announce in the group chat first (shared file).
public class Scholarship
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    // Who gives out this scholarship (university, government, company...).
    [Required, StringLength(200)]
    public string Provider { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    // ----- Eligibility rules (used by EligibilityService) -----

    // The lowest CGPA a student can have and still qualify. Example: 3.00
    [Precision(4, 2)]
    public decimal MinimumCgpa { get; set; }

    // The highest monthly household income allowed (in RM).
    // Null means "no income limit".
    [Precision(10, 2)]
    public decimal? MaximumHouseholdIncome { get; set; }

    // ----- Dates and visibility -----

    // Last day students can apply.
    public DateTime Deadline { get; set; }

    // Students only see the scholarship after an admin publishes it.
    public bool IsPublished { get; set; }

    // All applications sent in for this scholarship.
    public List<ScholarshipApplication> Applications { get; set; } = new();
}
