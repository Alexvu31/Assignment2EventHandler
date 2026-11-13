
using Assignment1EventHandler.Data;
using Assignment1EventHandler.Models;
using Assignment1EventHandler.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Assignment1EventHandler.Controllers
{
    [Route("events")]
    public class EventManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorage;

        public EventManagerController(
            ApplicationDbContext context,
            BlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // DISPLAY ALL EVENTS
        [HttpGet("")]
        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Attendees)
                .ToListAsync();

            return View(events);
        }

        // VIEW EVENT DETAILS
        [HttpGet("{eventId:int}/details")]
        public async Task<IActionResult> Details(int eventId)
        {
            var selectedEvent = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (selectedEvent == null)
                return NotFound();

            return View(selectedEvent);
        }

        // DISPLAY CREATE FORM
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE EVENT WITH AZURE BLOB IMAGE UPLOAD
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Event ev,
            IFormFile? bannerImage)
        {
            ModelState.Remove("Attendees");

            if (!ModelState.IsValid)
                return View(ev);

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

        // DISPLAY EDIT FORM
        [HttpGet("{eventId:int}/edit")]
        public async Task<IActionResult> Edit(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // UPDATE EVENT WITH AZURE BLOB IMAGE UPLOAD
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

        // DISPLAY DELETE CONFIRMATION
        [HttpGet("{eventId:int}/delete")]
        public async Task<IActionResult> Delete(int eventId)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // DELETE EVENT
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

        // DISPLAY ATTENDEES FOR AN EVENT
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

        // DISPLAY ADD ATTENDEE FORM
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

        // ADD ATTENDEE
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

        // DISPLAY EDIT ATTENDEE FORM
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

        // UPDATE ATTENDEE
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

        // DELETE ATTENDEE
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
