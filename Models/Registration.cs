namespace EventManagementSystem.Models;

public class Registration
{
    public int RegistrationId { get; set; }
    public int EventId { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Registered"; // Registered / Cancelled / Waitlisted
    public string QrToken { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }

    // Convenience fields populated by joins, not columns on this table
    public string? EventTitle { get; set; }
    public string? AttendeeName { get; set; }
    public string? AttendeeEmail { get; set; }
    public bool IsCheckedIn { get; set; }
}
