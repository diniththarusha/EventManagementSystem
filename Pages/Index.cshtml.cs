using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages;

public class IndexModel : PageModel
{
    private readonly EventService _eventService;

    public IndexModel(EventService eventService)
    {
        _eventService = eventService;
    }

    public List<Event> UpcomingEvents { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allEvents = await _eventService.GetAllAsync();
        UpcomingEvents = allEvents
            .Where(e => e.Status == "Scheduled" && e.EventDate >= DateTime.Now)
            .OrderBy(e => e.EventDate)
            .Take(3)
            .ToList();
    }
}
