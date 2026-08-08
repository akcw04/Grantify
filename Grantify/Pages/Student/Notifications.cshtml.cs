using Grantify.Models;
using Grantify.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grantify.Pages.Student;

// #4 - the messages telling the student what happened to their applications.
// The list is built from the officer audit trail; see StudentService.
public class NotificationsModel : StudentPageModel
{
    private readonly StudentService _students;

    public NotificationsModel(StudentService students)
    {
        _students = students;
    }

    public List<Notification> Notifications { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Notifications = await _students.GetNotificationsAsync(CurrentUserId);
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        await _students.MarkAllReadAsync(CurrentUserId);
        SetMessage("All notifications marked as read.");
        return RedirectToPage();
    }
}
