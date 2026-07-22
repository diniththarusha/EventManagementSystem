using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models;

public class Event
{
    public int EventId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(150)]
    public string? Venue { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime EventDate { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
    public int Capacity { get; set; }

    public string Status { get; set; } = "Scheduled"; // Scheduled / Cancelled / Completed

    public int? CreatedBy { get; set; }

    // Not persisted directly — computed by EventService for display
    public int RegisteredCount { get; set; }
}