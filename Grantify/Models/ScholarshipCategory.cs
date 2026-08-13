using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// A grouping label for scholarships (e.g. "STEM", "Sports", "Need-based").
public class ScholarshipCategory
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
}
