using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagementSystem.Data;
using EventManagementSystem.Models;

namespace EventManagementSystem.Pages.Registrations;

[Authorize(Roles = "Admin")]
public class AttendeesModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;
    private readonly NotificationService _notificationService;

    public AttendeesModel(EventService eventService, RegistrationService registrationService, NotificationService notificationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _notificationService = notificationService;
    }

    public Event Event { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public string? Search { get; set; }

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int eventId, string? search)
    {
        var ev = await _eventService.GetByIdAsync(eventId);
        if (ev is null)
        {
            return RedirectToPage("/Events/Index");
        }

        Event = ev;
        Search = search;

        var all = await _registrationService.GetByEventAsync(eventId);
        Registrations = string.IsNullOrWhiteSpace(search)
            ? all
            : all.Where(r =>
                (r.AttendeeName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.AttendeeEmail?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostCheckInAsync(int registrationId, int eventId)
    {
        var didCheckIn = await _registrationService.CheckInAsync(registrationId, "Manual");

        if (didCheckIn)
        {
            await NotifyCheckedInAsync(registrationId, eventId);
            Message = "Checked in successfully.";
        }
        else
        {
            Message = "This attendee was already checked in.";
        }

        return RedirectToPage(new { eventId });
    }

    public async Task<IActionResult> OnPostScanAsync(string token, int eventId)
    {
        var registration = await _registrationService.GetByQrTokenAsync(token);

        if (registration is null)
        {
            Message = "QR code not recognized.";
        }
        else if (registration.EventId != eventId)
        {
            Message = $"This ticket is for a different event ({registration.AttendeeName}).";
        }
        else
        {
            var didCheckIn = await _registrationService.CheckInAsync(registration.RegistrationId, "Scan");
            if (didCheckIn)
            {
                await NotifyCheckedInAsync(registration.RegistrationId, eventId);
                Message = $"Checked in: {registration.AttendeeName}";
            }
            else
            {
                Message = $"{registration.AttendeeName} was already checked in.";
            }
        }

        return RedirectToPage(new { eventId });
    }

    public async Task<IActionResult> OnGetExportCsvAsync(int eventId)
    {
        var ev = await _eventService.GetByIdAsync(eventId);
        if (ev is null)
        {
            return RedirectToPage("/Events/Index");
        }

        var registrations = await _registrationService.GetAttendanceExportAsync(eventId);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Name,Email,Status,Registered At,Checked In,Checked In At,Checked In Via");

        foreach (var r in registrations)
        {
            csv.AppendLine(string.Join(",",
                CsvEscape(r.AttendeeName),
                CsvEscape(r.AttendeeEmail),
                CsvEscape(r.Status),
                CsvEscape(r.RegisteredAt.ToString("yyyy-MM-dd HH:mm")),
                CsvEscape(r.IsCheckedIn ? "Yes" : "No"),
                CsvEscape(r.CheckedInAt?.ToString("yyyy-MM-dd HH:mm") ?? ""),
                CsvEscape(r.CheckedInVia ?? "")));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var safeTitle = string.Join("_", ev.Title.Split(Path.GetInvalidFileNameChars()));
        return File(bytes, "text/csv", $"{safeTitle}-attendance.csv");
    }

    private static string CsvEscape(string? value)
    {
        value ??= "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private async Task NotifyCheckedInAsync(int registrationId, int eventId)
    {
        var registration = await _registrationService.GetByRegistrationIdAsync(registrationId);
        var ev = await _eventService.GetByIdAsync(eventId);

        if (registration is not null && ev is not null)
        {
            await _notificationService.CreateAsync(registration.UserId, $"You're checked in for \"{ev.Title}\". Enjoy the event!");
        }
    }
}
