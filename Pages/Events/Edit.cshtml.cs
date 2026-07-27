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
    private readonly RegistrationService _registrationService;
    private readonly NotificationService _notificationService;

    public EditModel(EventService eventService, RegistrationService registrationService, NotificationService notificationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _notificationService = notificationService;
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

        var original = await _eventService.GetByIdAsync(Event.EventId);
        if (original is null)
        {
            return RedirectToPage("Index");
        }

        // Don't let capacity drop below the number of people already confirmed in.
        var activeCount = await _registrationService.GetActiveCountAsync(Event.EventId);
        if (Event.Capacity < activeCount)
        {
            ModelState.AddModelError(
                nameof(Event.Capacity),
                $"Capacity can't be less than the {activeCount} attendee(s) already registered.");
            return Page();
        }

        var dateChanged = original.EventDate != Event.EventDate;
        var venueChanged = !string.Equals(original.Venue, Event.Venue, StringComparison.OrdinalIgnoreCase);
        var capacityIncreased = Event.Capacity > original.Capacity;
        var justCancelled = original.Status != "Cancelled" && Event.Status == "Cancelled";

        await _eventService.UpdateAsync(Event);

        if (justCancelled)
        {
            // Cancellation trumps other change notices — tell every active/waitlisted attendee once.
            var attendees = await _registrationService.GetActiveAndWaitlistedByEventAsync(Event.EventId);
            foreach (var reg in attendees)
            {
                await _notificationService.CreateAsync(reg.UserId, $"'{Event.Title}' has been cancelled.");
            }
        }
        else
        {
            if (dateChanged || venueChanged)
            {
                var attendees = await _registrationService.GetActiveAndWaitlistedByEventAsync(Event.EventId);
                var changeParts = new List<string>();
                if (dateChanged) changeParts.Add($"new date/time is {Event.EventDate:MMM d, yyyy h:mm tt}");
                if (venueChanged) changeParts.Add($"new venue is {Event.Venue}");
                var message = $"'{Event.Title}' has been updated — {string.Join(" and ", changeParts)}.";

                foreach (var reg in attendees)
                {
                    await _notificationService.CreateAsync(reg.UserId, message);
                }
            }

            if (capacityIncreased)
            {
                var promoted = await _registrationService.PromoteWaitlistedAsync(Event.EventId, Event.Capacity);
                foreach (var reg in promoted)
                {
                    await _notificationService.CreateAsync(reg.UserId, $"You're off the waitlist for '{Event.Title}' — you're now confirmed!");
                }
            }
        }

        return RedirectToPage("Details", new { id = Event.EventId });
    }
}
