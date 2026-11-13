
using Microsoft.EntityFrameworkCore;
using Assignment1EventHandler.Models;

namespace Assignment1EventHandler.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            ApplicationDbContext context)
        {
            // Apply database migrations
            await context.Database.MigrateAsync();

            // Don't insert duplicate seed data
            if (await context.Events.AnyAsync())
            {
                return;
            }

            var events = new List<Event>
            {
                new Event
                {
                    Title = "ASP.NET Core Workshop",
                    Description = "Learn ASP.NET Core MVC and Entity Framework Core.",
                    Date = new DateTime(2026, 12, 5, 10, 0, 0),
                    Location = "Room 101",

                    Attendees = new List<Attendee>
                    {
                        new Attendee
                        {
                            Name = "John Smith",
                            Email = "john@example.com"
                        },
                        new Attendee
                        {
                            Name = "Mary Johnson",
                            Email = "mary@example.com"
                        }
                    }
                },

                new Event
                {
                    Title = "C# Programming Seminar",
                    Description = "Explore modern C# programming techniques.",
                    Date = new DateTime(2026, 12, 10, 13, 30, 0),
                    Location = "Room 202",

                    Attendees = new List<Attendee>
                    {
                        new Attendee
                        {
                            Name = "David Brown",
                            Email = "david@example.com"
                        },
                        new Attendee
                        {
                            Name = "Sarah Wilson",
                            Email = "sarah@example.com"
                        }
                    }
                },

                new Event
                {
                    Title = "Web Development Meetup",
                    Description = "Meet developers and discuss web technologies.",
                    Date = new DateTime(2026, 12, 15, 18, 0, 0),
                    Location = "Room 303",

                    Attendees = new List<Attendee>
                    {
                        new Attendee
                        {
                            Name = "Alex Green",
                            Email = "alex@example.com"
                        },
                        new Attendee
                        {
                            Name = "Emma White",
                            Email = "emma@example.com"
                        }
                    }
                }
            };

            await context.Events.AddRangeAsync(events);

            await context.SaveChangesAsync();
        }
    }
}
