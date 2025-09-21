using Microsoft.AspNetCore.SignalR;

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
    }
}

