using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Events;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly EventService _eventService;

    public DeleteModel(EventService eventService)
    {
        _eventService = eventService;
    }

    [BindProperty]
    public Event Event { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ev = await _eventService.GetByIdAsync(id);
        if (ev is null)
        {
            return RedirectToPage("Index");
        }

        Event = ev;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _eventService.DeleteAsync(Event.EventId);
        return RedirectToPage("Index");
    }
}
