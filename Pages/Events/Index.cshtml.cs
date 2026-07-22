using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Events;

public class IndexModel : PageModel
{
    private readonly EventService _eventService;

    public IndexModel(EventService eventService)
    {
        _eventService = eventService;
    }

    public List<Event> Events { get; set; } = new();

    public async Task OnGetAsync()
    {
        Events = await _eventService.GetAllAsync();
    }
}
