using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Services.Resume;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Escaleras_Serpientes.Hubs
{
    // DTOs simples para intercambiar con el cliente
    public sealed record ActionDetailsDto(string Type, object? Payload);
    public sealed record GameStateDto(string RoomId, object State, string CurrentTurnPlayer);
    public sealed record GameResultDto(string RoomId, object Results);
    public sealed record RankingEntryDto(string PlayerName, int Wins);
    public sealed record PlayerSnapDto(int PlayerId, string Name, int TurnOrder, int Position);
    public sealed record GameSnapshotDto(
        int RoomCode,
        string RoomName,
        int CurrentTurnOrder,
        IReadOnlyList<PlayerSnapDto> Players,
        IReadOnlyDictionary<int, int> Jumps
    );

    public interface IGameService
    {
        Task<GameStateDto> ApplyActionAsync(string roomId, string playerName, ActionDetailsDto action, CancellationToken ct = default);
        Task<(string NextPlayerName, GameStateDto State)> AdvanceTurnAsync(string roomId, CancellationToken ct = default);
        Task<bool> IsGameEndedAsync(string roomId, out GameResultDto? results);
        Task<IReadOnlyList<RankingEntryDto>> GetRankingAsync(CancellationToken ct = default);
    }

    public class GameHub : Hub
    {
        private readonly ConnectionRegistryGame _registry;  // <-- usa tu ConnectionRegistry
        private readonly IGameService _game;
        private readonly IResumeService _resume;   // <── nuevo

        public GameHub(ConnectionRegistryGame registry, IGameService game, IResumeService resume)
        {
            _registry = registry;
            _game = game;
            _resume = resume;
        }

        // Utilidad: nombre amigable desde claims si no viene explícito
        private string ResolveDisplayName(string? providedName)
        {
            if (!string.IsNullOrWhiteSpace(providedName)) return providedName!;
            var nameClaim = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                            ?? Context.User?.Identity?.Name;
            return string.IsNullOrWhiteSpace(nameClaim) ? Context.ConnectionId : nameClaim!;
        }

        // Llamado desde el tablero tras navegar
        public async Task SubscribeGame(string groupId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new HubException("groupId requerido");

            var who = string.IsNullOrWhiteSpace(playerName)
                ? (Context.User?.Identity?.Name ?? Context.ConnectionId)
                : playerName;

            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            _registry.Set(Context.ConnectionId, groupId, who);

            // tomar el estado actual de la partida
            var snap = await _resume.GetSnapshotAsync(groupId);

            // enviar snapshot SOLO al caller (para hidratar el tablero)
            await Clients.Caller.SendAsync("GameSnapshot", snap);
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                throw new HubException("roomId requerido");

            var who = ResolveDisplayName(playerName);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            _registry.Set(Context.ConnectionId, roomId, who);

            // Notifica a TODOS en la sala que entró alguien
            await Clients.Group(roomId).SendAsync("PlayerJoined", who);

            // (Opcional) devolver lista de jugadores actuales a quien se unió
            var players = _registry.GetPlayersInRoom(roomId);
            await Clients.Caller.SendAsync("SystemMessage", $"Te uniste a la sala {roomId}");
            await Clients.Caller.SendAsync("PlayersList", players);
        }

        public async Task LeaveRoom(string roomId)
        {
            if (!_registry.IsInRoom(Context.ConnectionId, roomId))
                return;

            string playerName;
            if (_registry.TryGet(Context.ConnectionId, out var info) && info is not null)
                playerName = info.PlayerName;
            else
                playerName = Context.ConnectionId;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            _registry.Remove(Context.ConnectionId, out _);

            await Clients.Group(roomId).SendAsync("PlayerLeft", playerName);
        }

        // Chat opcional
        public async Task SendMessage(string roomId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (!_registry.IsInRoom(Context.ConnectionId, roomId))
                throw new HubException("No perteneces a esta sala.");

            var user = (_registry.TryGet(Context.ConnectionId, out var info) && info is not null)
                ? info.PlayerName
                : ResolveDisplayName(null);

            await Clients.Group(roomId).SendAsync("ReceiveChatMessage", user, message);
        }

        // Acción de juego: aplica acción, publica estado, y si cambia turno/termina la partida, notifica
        public async Task PerformGameAction(string roomId, ActionDetailsDto actionDetails)
        {
            if (!_registry.IsInRoom(Context.ConnectionId, roomId))
                throw new HubException("No perteneces a esta sala.");

            var player = (_registry.TryGet(Context.ConnectionId, out var info) && info is not null)
                ? info.PlayerName
                : ResolveDisplayName(null);

            // 1) aplicar acción al estado del juego
            var newState = await _game.ApplyActionAsync(roomId, player, actionDetails);

            // 2) broadcast estado
            await Clients.Group(roomId).SendAsync("GameUpdated", newState);

            // 3) verificar fin de juego
            if (await _game.IsGameEndedAsync(roomId, out var results) && results is not null)
            {
                await Clients.Group(roomId).SendAsync("GameEnded", results);
                // (Opcional) actualizar ranking global
                var ranking = await _game.GetRankingAsync();
                await Clients.All.SendAsync("RankingUpdated", ranking);
                return;
            }

            // 4) si no terminó, avanzar turno y notificar
            var (nextPlayer, stateAfterTurn) = await _game.AdvanceTurnAsync(roomId);
            await Clients.Group(roomId).SendAsync("TurnChanged", nextPlayer);
            await Clients.Group(roomId).SendAsync("GameUpdated", stateAfterTurn);
        }

        // Actualiza ranking global (puedes llamarlo tras partidas o desde admin)
        public async Task UpdateRanking()
        {
            var ranking = await _game.GetRankingAsync();
            await Clients.All.SendAsync("RankingUpdated", ranking);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_registry.Remove(Context.ConnectionId, out var removed) && removed is not null)
            {
                await Clients.Group(removed.RoomId).SendAsync("PlayerLeft", removed.PlayerName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, removed.RoomId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
