using Assignment1EventHandler.Data;
using System.ComponentModel.DataAnnotations;

namespace Assignment1EventHandler.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    public string BannerUrl { get; set; } = string.Empty;

    // Nullable for events created before Assignment 3 migration.
    public string? OrganizerUserId { get; set; }
    public ApplicationUser? Organizer { get; set; }

    public List<Attendee> Attendees { get; set; } = new();
}
