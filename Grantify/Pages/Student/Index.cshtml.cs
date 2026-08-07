using Grantify.Services;

namespace Grantify.Pages.Student;

// Landing page of the Student area: a summary, and where to go next.
// Access rule: only the "Student" role can open pages in this folder (Program.cs).
public class IndexModel : StudentPageModel
{
    private readonly StudentService _students;

    public IndexModel(StudentService students)
    {
        _students = students;
    }

    public StudentService.StudentDashboard Dashboard { get; private set; } = new();

    // The three most recent applications, shown under the tiles.
    public List<Grantify.Models.ScholarshipApplication> Recent { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard = await _students.GetDashboardAsync(CurrentUserId);

        var applications = await _students.GetMyApplicationsAsync(CurrentUserId);
        Recent = applications.Take(3).ToList();
    }
}
