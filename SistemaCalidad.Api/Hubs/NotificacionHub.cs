using Microsoft.AspNetCore.SignalR;

namespace SistemaCalidad.Api.Hubs;

[Microsoft.AspNetCore.Authorization.Authorize]
public class NotificacionHub : Hub
{
    public async Task SendNotification(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", user, message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
