using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Events;

public class DetailsModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;
    private readonly NotificationService _notificationService;

    public DetailsModel(EventService eventService, RegistrationService registrationService, NotificationService notificationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _notificationService = notificationService;
    }

    public Event Event { get; set; } = new();
    public Registration? MyRegistration { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ev = await _eventService.GetByIdAsync(id);
        if (ev is null)
        {
            return RedirectToPage("Index");
        }

        Event = ev;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            MyRegistration = await _registrationService.GetByEventAndUserAsync(id, userId);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login");
        }

        var ev = await _eventService.GetByIdAsync(eventId);
        if (ev is null)
        {
            return RedirectToPage("Index");
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existing = await _registrationService.GetByEventAndUserAsync(eventId, userId);
        if (existing is null)
        {
            var registration = await _registrationService.RegisterAsync(eventId, userId, ev.Capacity);

            var message = registration.Status == "Waitlisted"
                ? $"You're on the waitlist for \"{ev.Title}\". We'll notify you if a spot opens up."
                : $"You're registered for \"{ev.Title}\". Your QR ticket is ready under My Registrations.";
            await _notificationService.CreateAsync(userId, message);
        }

        return RedirectToPage("Details", new { id = eventId });
    }
}

