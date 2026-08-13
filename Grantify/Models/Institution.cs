using System.ComponentModel.DataAnnotations;

namespace Grantify.Models;

// A school, university or organisation linked to a scholarship.
public class Institution
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [StringLength(256)]
    public string ContactEmail { get; set; } = string.Empty;
}
