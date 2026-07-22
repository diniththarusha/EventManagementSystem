using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;

    public DashboardModel(EventService eventService, RegistrationService registrationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
    }

    public DashboardStats Stats { get; set; } = new();
    public List<Event> Events { get; set; } = new();

    public async Task OnGetAsync()
    {
        Stats = await _registrationService.GetDashboardStatsAsync();
        Events = await _eventService.GetAllAsync();
    }
}
