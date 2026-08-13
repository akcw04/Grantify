using Grantify.Services;

namespace Grantify.Pages.Admin.Users;

public class IndexModel : AdminPageModel
{
    private readonly AdminUserService _users;

    public IndexModel(AdminUserService users)
    {
        _users = users;
    }

    public List<UserListItem> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _users.GetAllUsersAsync();
    }
}
