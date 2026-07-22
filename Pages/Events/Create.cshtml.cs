using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Events;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly EventService _eventService;

    public CreateModel(EventService eventService)
    {
        _eventService = eventService;
    }

    [BindProperty]
    public Event Event { get; set; } = new() { EventDate = DateTime.Now.AddDays(7) };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _eventService.CreateAsync(Event, userId);
        return RedirectToPage("Index");
    }
}
