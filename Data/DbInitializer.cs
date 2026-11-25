using Assignment1EventHandler.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Assignment1EventHandler.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { "Organizer", "Attendee" })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Could not create {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        var organizer = await SeedUser(users, config,
            "IdentitySeed:OrganizerEmail", "IdentitySeed:OrganizerPassword",
            "organizer@example.com", "Organizer123!", "Organizer");

        // Prefer the new Attendee config keys, but support the old Lab 6 User keys.
        var attendeeEmail = config["IdentitySeed:AttendeeEmail"] ?? config["IdentitySeed:UserEmail"] ?? "user@example.com";
        var attendeePassword = config["IdentitySeed:AttendeePassword"] ?? config["IdentitySeed:UserPassword"] ?? "User123!";
        var attendee = await SeedUser(users, attendeeEmail, attendeePassword, "Attendee");

        // Convert existing Lab 6 accounts from the old User role to Attendee.
        if (await roles.RoleExistsAsync("User"))
        {
            foreach (var oldUser in await users.GetUsersInRoleAsync("User"))
            {
                if (!await users.IsInRoleAsync(oldUser, "Attendee"))
                    await EnsureRole(users, oldUser, "Attendee");
            }
        }

        // Associate legacy events with the seeded organizer so private notifications work.
        var existingEvents = await context.Events.Where(e => e.OrganizerUserId == null).ToListAsync();
        foreach (var ev in existingEvents)
            ev.OrganizerUserId = organizer.Id;

        if (!await context.Events.AnyAsync())
        {
            context.Events.AddRange(
                new Event
                {
                    Title = "ASP.NET Core Workshop",
                    Description = "Learn ASP.NET Core MVC and Entity Framework Core.",
                    Date = new DateTime(2026, 12, 5, 10, 0, 0),
                    Location = "Room 101",
                    OrganizerUserId = organizer.Id,
                    Attendees = new List<Attendee>
                    {
                        new() { Name = "John Smith", Email = "john@example.com" },
                        new() { Name = "Mary Johnson", Email = "mary@example.com" }
                    }
                },
                new Event
                {
                    Title = "C# Programming Seminar",
                    Description = "Explore modern C# programming techniques.",
                    Date = new DateTime(2026, 12, 10, 13, 30, 0),
                    Location = "Room 202",
                    OrganizerUserId = organizer.Id,
                    Attendees = new List<Attendee>
                    {
                        new() { Name = "David Brown", Email = "david@example.com" },
                        new() { Name = "Sarah Wilson", Email = "sarah@example.com" }
                    }
                });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> SeedUser(
        UserManager<ApplicationUser> manager, IConfiguration config,
        string emailKey, string passwordKey, string defaultEmail,
        string defaultPassword, string role)
    {
        var email = config[emailKey] ?? defaultEmail;
        var password = config[passwordKey] ?? defaultPassword;
        return await SeedUser(manager, email, password, role);
    }

    private static async Task<ApplicationUser> SeedUser(
        UserManager<ApplicationUser> manager, string email,
        string password, string role)
    {
        var user = await manager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var create = await manager.CreateAsync(user, password);
            if (!create.Succeeded)
                throw new InvalidOperationException($"Could not seed {role}: {string.Join(", ", create.Errors.Select(e => e.Description))}");
        }
        await EnsureRole(manager, user, role);
        return user;
    }

    private static async Task EnsureRole(UserManager<ApplicationUser> manager, ApplicationUser user, string role)
    {
        if (!await manager.IsInRoleAsync(user, role))
        {
            var result = await manager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not assign {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
