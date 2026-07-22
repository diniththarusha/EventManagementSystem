using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly NotificationService _notificationService;

    public IndexModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public List<Notification> Notifications { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Notifications = await _notificationService.GetByUserAsync(userId);
        await _notificationService.MarkAllReadAsync(userId);
    }
}
