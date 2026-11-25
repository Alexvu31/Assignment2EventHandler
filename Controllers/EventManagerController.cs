
using Assignment1EventHandler.Data;
using Assignment1EventHandler.Models;
using Assignment1EventHandler.Services;
using Assignment1EventHandler.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Assignment1EventHandler.Controllers
{
    [Authorize]
    [Route("events")]
    public class EventManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorage;
        private readonly IHubContext<EventHub> _hub;

        public EventManagerController(
            ApplicationDbContext context,
            BlobStorageService blobStorage,
            IHubContext<EventHub> hub)
        {
            _context = context;
            _blobStorage = blobStorage;
            _hub = hub;
        }

        // DISPLAY ALL EVENTS - ORGANIZER AND USER
        [HttpGet("")]
        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Attendees)
                .ToListAsync();

            return View(events);
        }

        // VIEW EVENT DETAILS - ORGANIZER AND USER
        [HttpGet("{eventId:int}/details")]
        public async Task<IActionResult> Details(int eventId)
        {
            var selectedEvent = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (selectedEvent == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsRegistered = currentUserId != null && selectedEvent.Attendees.Any(a => a.UserId == currentUserId);
            ViewBag.AttendeeCount = selectedEvent.Attendees.Count;
            return View(selectedEvent);
        }

        // DISPLAY CREATE FORM - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE EVENT - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Event ev,
            IFormFile? bannerImage)
        {
            ModelState.Remove("Attendees");
            ModelState.Remove("Organizer");
            ModelState.Remove("OrganizerUserId");

            if (!ModelState.IsValid)
                return View(ev);

            // Never accept event ownership from posted form data.
            ev.OrganizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Upload banner to Azure Blob Storage
            if (bannerImage != null && bannerImage.Length > 0)
            {
                try
                {
                    ev.BannerUrl =
                        await _blobStorage.UploadImageAsync(bannerImage);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return View(ev);
                }
            }

            // Save event and banner URL to Azure SQL
            _context.Events.Add(ev);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DISPLAY EDIT FORM - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("{eventId:int}/edit")]
        public async Task<IActionResult> Edit(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // UPDATE EVENT - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("{eventId:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int eventId,
            Event ev,
            IFormFile? bannerImage)
        {
            if (eventId != ev.Id)
                return BadRequest();

            ModelState.Remove("Attendees");

            var existingEvent = await _context.Events
                .FindAsync(eventId);

            if (existingEvent == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ev.BannerUrl = existingEvent.BannerUrl;
                return View(ev);
            }

            // Upload replacement banner only if a new image was selected
            if (bannerImage != null && bannerImage.Length > 0)
            {
                try
                {
                    existingEvent.BannerUrl =
                        await _blobStorage.UploadImageAsync(bannerImage);
                }
                catch (InvalidOperationException ex)
                {
                    ev.BannerUrl = existingEvent.BannerUrl;
                    ModelState.AddModelError("", ex.Message);
                    return View(ev);
                }
            }

            // Update existing event information
            existingEvent.Title = ev.Title;
            existingEvent.Description = ev.Description;
            existingEvent.Date = ev.Date;
            existingEvent.Location = ev.Location;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details), new { eventId });
        }

        // DISPLAY DELETE CONFIRMATION - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("{eventId:int}/delete")]
        public async Task<IActionResult> Delete(int eventId)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // DELETE EVENT - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("{eventId:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            _context.Events.Remove(ev);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ATTENDEE SELF-REGISTRATION - ANY AUTHENTICATED USER
        [HttpPost("{eventId:int}/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var ev = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (await _context.Attendees.AnyAsync(a => a.EventId == eventId && a.UserId == userId))
            {
                TempData["RegistrationMessage"] = "You are already registered for this event.";
                return RedirectToAction(nameof(Details), new { eventId });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Challenge();

            var attendee = new Attendee
            {
                EventId = eventId,
                UserId = userId,
                Name = user.UserName ?? user.Email ?? "Attendee",
                Email = user.Email ?? string.Empty
            };
            _context.Attendees.Add(attendee);
            await _context.SaveChangesAsync();

            var attendeeCount = await _context.Attendees.CountAsync(a => a.EventId == eventId);
            await _hub.Clients.Group($"event-{eventId}").SendAsync("AttendeeUpdated", new
            {
                attendeeCount,
                attendeeId = attendee.Id,
                attendeeName = attendee.Name,
                attendeeEmail = attendee.Email
            });

            if (!string.IsNullOrWhiteSpace(ev.OrganizerUserId))
            {
                await _hub.Clients.User(ev.OrganizerUserId).SendAsync(
                    "OrganizerNotification",
                    $"{attendee.Email} just registered for your {ev.Title}.");
            }

            TempData["RegistrationMessage"] = "You have successfully registered.";
            return RedirectToAction(nameof(Details), new { eventId });
        }

        // ATTENDEE SELF-UNREGISTRATION - ONLY DELETE THE CURRENT USER'S RECORD
        [HttpPost("{eventId:int}/unregister")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var attendee = await _context.Attendees.FirstOrDefaultAsync(a =>
                a.EventId == eventId && a.UserId == userId);
            if (attendee == null) return NotFound();

            _context.Attendees.Remove(attendee);
            await _context.SaveChangesAsync();

            var count = await _context.Attendees.CountAsync(a => a.EventId == eventId);
            await _hub.Clients.Group($"event-{eventId}").SendAsync("AttendeeRemoved", new
            {
                attendeeId = attendee.Id,
                attendeeCount = count
            });

            return RedirectToAction(nameof(Details), new { eventId });
        }

        // DISPLAY ATTENDEES - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("{eventId:int}/attendees")]
        public async Task<IActionResult> ManageAttendees(int eventId)
        {
            var selectedEvent = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (selectedEvent == null)
                return NotFound();

            return View(selectedEvent);
        }

        // DISPLAY ADD ATTENDEE FORM - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("{eventId:int}/attendees/create")]
        public async Task<IActionResult> CreateAttendee(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            ViewBag.EventName = ev.Title;

            return View("CreateAttendee",
                new Attendee { EventId = eventId });
        }

        // ADD ATTENDEE - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("{eventId:int}/attendees/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttendee(
            int eventId, Attendee attendee)
        {
            ModelState.Remove("Event");

            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.EventName = ev.Title;
                return View("CreateAttendee", attendee);
            }

            attendee.EventId = eventId;

            _context.Attendees.Add(attendee);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ManageAttendees), new { eventId });
        }

        // DISPLAY EDIT ATTENDEE FORM - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpGet("{eventId:int}/attendees/{id}/edit")]
        public async Task<IActionResult> EditAttendee(
            int eventId, string id)
        {
            var attendee = await _context.Attendees
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.EventId == eventId);

            if (attendee == null)
                return NotFound();

            return View(attendee);
        }

        // UPDATE ATTENDEE - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("{eventId:int}/attendees/{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttendee(
            int eventId, string id, Attendee attendee)
        {
            ModelState.Remove("Event");

            var existing = await _context.Attendees
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.EventId == eventId);

            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(attendee);

            existing.Name = attendee.Name;
            existing.Email = attendee.Email;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ManageAttendees), new { eventId });
        }

        // DELETE ATTENDEE - ORGANIZER ONLY
        [Authorize(Roles = "Organizer")]
        [HttpPost("{eventId:int}/attendees/{id}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendee(
            int eventId, string id)
        {
            var attendee = await _context.Attendees
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.EventId == eventId);

            if (attendee == null)
                return NotFound();

            _context.Attendees.Remove(attendee);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ManageAttendees), new { eventId });
        }
    }
}
