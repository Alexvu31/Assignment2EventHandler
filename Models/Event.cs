using System.ComponentModel.DataAnnotations;

namespace Assignment1EventHandler.Models
{
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

        public List<Attendee> Attendees { get; set; }
            = new List<Attendee>();
    }
}