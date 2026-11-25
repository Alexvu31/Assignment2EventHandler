using Assignment1EventHandler.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Assignment1EventHandler.Hubs;

[Authorize]
public class EventHub : Hub
{
    private readonly ApplicationDbContext _context;

    public EventHub(ApplicationDbContext context) => _context = context;

    public async Task JoinEventGroup(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.Id == eventId))
            throw new HubException("Event not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }

    public Task LeaveEventGroup(int eventId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
}
