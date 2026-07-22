using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Events;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly EventService _eventService;

    public EditModel(EventService eventService)
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
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _eventService.UpdateAsync(Event);
        return RedirectToPage("Details", new { id = Event.EventId });
    }
}
