using Grantify.Models;
using Grantify.Services;

namespace Grantify.Pages.Student;

// #4 - the list of everything this student has applied for, and where each one
// has got to. The status is set by the officer; the student only reads it here.
public class ApplicationsModel : StudentPageModel
{
    private readonly StudentService _students;

    public ApplicationsModel(StudentService students)
    {
        _students = students;
    }

    public List<ScholarshipApplication> Applications { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Applications = await _students.GetMyApplicationsAsync(CurrentUserId);
    }
}
