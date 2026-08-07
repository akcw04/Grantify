using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Student;

// #3 - one application: its details, and the supporting documents the student
// uploads for it. Also the page an officer's decision eventually shows up on.
public class ApplyModel : StudentPageModel
{
    private readonly StudentService _students;

    public ApplyModel(StudentService students)
    {
        _students = students;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public ScholarshipApplication? Application { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Application = await _students.GetApplicationAsync(CurrentUserId, Id);

        // Not found, or it belongs to somebody else - same answer either way.
        if (Application is null) return RedirectToPage("Applications");

        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (Upload is null)
        {
            SetMessage("Choose a file first.", true);
            return RedirectToPage(new { Id });
        }

        var result = await _students.SaveDocumentAsync(CurrentUserId, Id, Upload);
        SetMessage(result.Message, !result.Success);

        return RedirectToPage(new { Id });
    }

    // Serves a file the student uploaded. The service checks they own it.
    public async Task<IActionResult> OnGetDownloadAsync(int documentId)
    {
        var file = await _students.GetMyDocumentAsync(CurrentUserId, documentId);
        if (file is null) return NotFound();

        return PhysicalFile(file.Value.FullPath, "application/octet-stream", file.Value.FileName);
    }
}
