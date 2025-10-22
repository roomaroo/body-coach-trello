using Microsoft.AspNetCore.SignalR;

namespace BodyCoachTrello.Web.Hubs;

/// <summary>
/// SignalR hub for real-time shopping list import progress updates
/// </summary>
public class ShoppingListProgressHub : Hub
{
    /// <summary>
    /// Join a specific progress group for tracking import progress
    /// </summary>
    /// <param name="connectionId">Unique connection ID for this import session</param>
    public async Task JoinProgressGroup(string connectionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"progress_{connectionId}");
    }

    /// <summary>
    /// Leave a progress group when import is complete or user disconnects
    /// </summary>
    /// <param name="connectionId">Unique connection ID for this import session</param>
    public async Task LeaveProgressGroup(string connectionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"progress_{connectionId}");
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}