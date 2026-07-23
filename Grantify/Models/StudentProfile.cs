using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Grantify.Models;

// Extra details about a student that the login account does not hold.
// The eligibility check compares these numbers against a scholarship's rules.
// One student account has exactly one profile.
public class StudentProfile
{
    public int Id { get; set; }

    // Which login account this profile belongs to.
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Current CGPA, e.g. 3.75
    [Precision(4, 2)]
    public decimal Cgpa { get; set; }

    // Monthly household income in RM. Used for need-based scholarships.
    [Precision(10, 2)]
    public decimal HouseholdIncome { get; set; }

    // What the student is studying, e.g. "Software Engineering".
    [StringLength(200)]
    public string Course { get; set; } = string.Empty;

    // Where the student studies, e.g. "Asia Pacific University".
    [StringLength(200)]
    public string Institution { get; set; } = string.Empty;
}
