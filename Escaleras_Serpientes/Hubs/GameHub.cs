using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Escaleras_Serpientes.Hubs
{
    public class GameHub: Hub
    {
        public async Task SendMessage(string user, string message, string wins, int winsNumber)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message, wins, winsNumber);
        }

        public async Task SendChatMessage(string sala, string user, string message)
        {
            await Clients.Group(sala).SendAsync("ReceiveChatMessage", user, message);
        }

        public async Task UpdateRanking()
        {
            await Clients.All.SendAsync("ReceiveChatMessage");
        }

        public async Task JoinRoom(string groupName, string? displayName = null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // nombre amigable
            var who = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : (Context.User?.FindFirst(ClaimTypes.Name)?.Value
                   ?? Context.User?.Identity?.Name
                   ?? Context.ConnectionId);

            // Notificas a los demás de la sala
            await Clients.OthersInGroup(groupName)
                .SendAsync("PlayerJoined", new { group = groupName, player = who });

            // Mensaje para el que se unió
            await Clients.Caller
                .SendAsync("SystemMessage", $"Te uniste a la sala {groupName}");
        }

        public async Task LeaveRoom(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("SystemMessage", $"Jugador {Context.ConnectionId} salió.");
        }
    }
}

