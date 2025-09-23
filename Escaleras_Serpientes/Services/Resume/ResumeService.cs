using Escaleras_Serpientes.Hubs;
using Escaleras_Serpientes.Services.Player;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Escaleras_Serpientes.Services.Resume
{

    public class ResumeService : IResumeService
    {
        private readonly SnakesLaddersDbContext _dbContext;
        private readonly IPlayerService _playerService;
        private readonly IHubContext<GameHub> _hub;

        public ResumeService(
            SnakesLaddersDbContext dbContext,
            IPlayerService playerService,
            IHubContext<GameHub> hub)
        {
            _dbContext = dbContext;
            _playerService = playerService;
            _hub = hub;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Config del juego
        // ─────────────────────────────────────────────────────────────────────
        private const int FinalCell = 30;

        // Lock por sala (roomCode) para serializar acciones
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _roomLocks = new();

        // Mapa de serpientes/escaleras: celda -> destino
        private static readonly Dictionary<int, int> _jumps = new()
        {
            // Escaleras
            { 3, 22 }, { 5, 8 }, { 11, 26 }, { 20, 29 },
            // Serpientes
            { 27, 1 }, { 21, 9 }, { 17, 4 }, { 19, 7 },
            // TODO: agrega las reales de tu tablero...
        };

        // ─────────────────────────────────────────────────────────────────────
        // 1) Inicializar partida
        // ─────────────────────────────────────────────────────────────────────
        public async Task InitializeGameAsync(int roomCode, int startedByUserId, CancellationToken ct = default)
        {
            var sem = _roomLocks.GetOrAdd(roomCode, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                var room = await _dbContext.Rooms
                    .Include(r => r.RoomPlayers).ThenInclude(rp => rp.Player)
                    .SingleOrDefaultAsync(r => r.Code == roomCode, ct)
                    ?? throw new KeyNotFoundException("Sala no encontrada.");

                if (room.IsStarted)
                    throw new InvalidOperationException("La partida ya fue iniciada.");

                if (room.RoomPlayers.Count < 2)
                    throw new InvalidOperationException("Se requieren al menos 2 jugadores.");

                // Orden y posiciones iniciales
                int order = 0;
                foreach (var rp in room.RoomPlayers.OrderBy(rp => rp.Id))
                {
                    rp.TurnOrder = order++;
                    rp.Position = 0;
                }

                room.CurrentTurnOrder = 0;
                room.IsStarted = true;

                await _dbContext.SaveChangesAsync(ct);

                // Aviso de inicio a la sala (grupo = room.Name)
                var group = room.Code.ToString();

                await _hub.Clients.Group(group).SendAsync("GameStarted", new
                {
                    roomCode = room.Code,
                    roomName = room.Name,
                    Players = room.RoomPlayers.Select(rp => new {
                        rp.PlayerId,
                        rp.Player.Name,
                        rp.TurnOrder,
                        rp.Position
                    }),
                    firstTurnOrder = room.CurrentTurnOrder
                }, ct);
            }
            finally
            {
                sem.Release();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2) Jugar turno
        // ─────────────────────────────────────────────────────────────────────
        public async Task PlayTurnAsync(int roomCode, int userId, CancellationToken ct = default)
        {
            var sem = _roomLocks.GetOrAdd(roomCode, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                var room = await _dbContext.Rooms
                    .Include(r => r.RoomPlayers).ThenInclude(rp => rp.Player)
                    .SingleOrDefaultAsync(r => r.Code == roomCode, ct)
                    ?? throw new KeyNotFoundException("Sala no encontrada.");

                if (!room.IsStarted)
                    throw new InvalidOperationException("La partida no está iniciada.");

                var me = await _dbContext.Players
                    .SingleOrDefaultAsync(p => p.Id == userId, ct)
                    ?? throw new InvalidOperationException("Jugador no existe.");

                var myRP = room.RoomPlayers
                    .SingleOrDefault(rp => rp.PlayerId == me.Id)
                    ?? throw new InvalidOperationException("No estás en esta sala.");

                // Validar turno
                if (myRP.TurnOrder != room.CurrentTurnOrder)
                    throw new InvalidOperationException("No es tu turno.");

                // Tirar dado
                int dice = Random.Shared.Next(1, 7);

                int from = myRP.Position;
                int tentative = from + dice;

                // Regla: si te pasas, no avanzas
                bool overshoot = tentative > FinalCell;
                if (overshoot) tentative = from;

                myRP.Position = tentative;

                // Serpientes / escaleras
                if (_jumps.TryGetValue(myRP.Position, out var jumpTo))
                {
                    int pre = myRP.Position;
                    myRP.Position = jumpTo;

                    await _dbContext.SaveChangesAsync(ct); // persistir antes del evento

                    await _hub.Clients.Group(room.Name).SendAsync("SnakesLaddersHit", new
                    {
                        playerId = me.Id,
                        from = pre,
                        to = myRP.Position
                    }, ct);
                }

                await _dbContext.SaveChangesAsync(ct); // movimiento base

                // 🔔 Resultado del dado (siempre)
                await _hub.Clients.Group(room.Name).SendAsync("DiceRolled", new
                {
                    playerId = me.Id,
                    playerName = me.Name,
                    dice,
                    from,
                    to = myRP.Position,
                    overshoot
                }, ct);

                // ¿Ganó?
                if (myRP.Position >= FinalCell)
                {
                    _playerService.AddWin(me.Id);

                    await _hub.Clients.Group(room.Name).SendAsync("PlayerWon", new
                    {
                        playerId = me.Id,
                        name = me.Name
                    }, ct);

                    room.IsStarted = false;
                    await _dbContext.SaveChangesAsync(ct);
                    return;
                }

                // Siguiente turno (circular)
                int maxTurn = room.RoomPlayers.Max(rp => rp.TurnOrder);
                room.CurrentTurnOrder = (room.CurrentTurnOrder == maxTurn) ? 0 : room.CurrentTurnOrder + 1;

                await _dbContext.SaveChangesAsync(ct);

                // 🔔 Notificar siguiente turno e incluir el último dado
                await _hub.Clients.Group(room.Name).SendAsync("NextTurn", new
                {
                    turnOrder = room.CurrentTurnOrder,
                    lastDice = new
                    {
                        playerId = me.Id,
                        playerName = me.Name,
                        dice,
                        from,
                        to = myRP.Position,
                        overshoot
                    }
                }, ct);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<GameSnapshotDto> GetSnapshotAsync(string groupId, CancellationToken ct = default)
        {
            // groupId es el nombre del grupo que usas en SignalR. 
            // En tu servicio envías a Clients.Group(room.Name), así que aquí buscamos por Name.
            // Si quieres soportar código, puedes intentar parsear a int y buscar por Code.
            var room = await _dbContext.Rooms
                .Include(r => r.RoomPlayers)
                    .ThenInclude(rp => rp.Player)
                .SingleOrDefaultAsync(r => r.Name == groupId, ct);

            if (room is null)
                throw new KeyNotFoundException("Sala no encontrada para el snapshot.");

            var players = room.RoomPlayers
                .OrderBy(rp => rp.TurnOrder)
                .Select(rp => new PlayerSnapDto(
                    rp.PlayerId,
                    rp.Player.Name,
                    rp.TurnOrder,
                    rp.Position
                ))
                .ToList();

            // Reusa tu diccionario de saltos del servicio
            var jumps = new Dictionary<int, int>(_jumps);

            return new GameSnapshotDto(
                RoomCode: room.Code,
                RoomName: room.Name,
                CurrentTurnOrder: room.CurrentTurnOrder,
                Players: players,
                Jumps: jumps
            );
        }

    }
}
