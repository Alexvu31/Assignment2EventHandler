

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Assignment1EventHandler.Models;

namespace Assignment1EventHandler.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider)
        {
            var context = serviceProvider
                .GetRequiredService<ApplicationDbContext>();

            var roleManager = serviceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = serviceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var configuration = serviceProvider
                .GetRequiredService<IConfiguration>();

            // Apply database migrations
            await context.Database.MigrateAsync();

            // Create roles
            string[] roles = { "Organizer", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new Exception(
                            $"Failed to create role {role}: " +
                            string.Join(", ",
                                result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Seed Organizer account
            await SeedUser(
                userManager,
                configuration,
                "IdentitySeed:OrganizerEmail",
                "IdentitySeed:OrganizerPassword",
                "organizer@example.com",
                "Organizer123!",
                "Organizer");

            // Seed regular User account
            await SeedUser(
                userManager,
                configuration,
                "IdentitySeed:UserEmail",
                "IdentitySeed:UserPassword",
                "user@example.com",
                "User123!",
                "User");

            // Seed sample events only when none exist
            if (!await context.Events.AnyAsync())
            {
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

        private static async Task SeedUser(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            string emailKey,
            string passwordKey,
            string defaultEmail,
            string defaultPassword,
            string role)
        {
            var email = configuration[emailKey] ?? defaultEmail;
            var password = configuration[passwordKey] ?? defaultPassword;

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    user, password);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        $"Failed to create {role} account: " +
                        string.Join(", ",
                            result.Errors.Select(e => e.Description)));
                }
            }

            // Ensure existing users have the correct role
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var result = await userManager.AddToRoleAsync(
                    user, role);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        $"Failed to assign {role} role: " +
                        string.Join(", ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
