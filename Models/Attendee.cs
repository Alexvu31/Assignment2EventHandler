
using System.ComponentModel.DataAnnotations;

namespace Assignment1EventHandler.Models
{
    public class Attendee
    {
        public string Id { get; set; } =
            Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public int EventId { get; set; }

        public Event? Event { get; set; }
    }
}
