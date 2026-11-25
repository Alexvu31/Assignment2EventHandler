using System.ComponentModel.DataAnnotations;
using Assignment1EventHandler.Data;

namespace Assignment1EventHandler.Models;

public class Attendee
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public int EventId { get; set; }
    public Event? Event { get; set; }

    // Null for manually added legacy/organizer-managed attendees.
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
