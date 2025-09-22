
using Escaleras_Serpientes.Hubs;
using Escaleras_Serpientes.Services.Auth;
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
        private readonly IPlayerService _PalyerService;
        private readonly IHubContext<GameHub> _hub;

        public ResumeService(SnakesLaddersDbContext dbContext, IPlayerService playerService, IHubContext<GameHub> hub)
        {
            _dbContext = dbContext;
            _PalyerService = playerService;
            _hub = hub;
        }
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _roomLocks = new();

        private static readonly Dictionary<int, int> _jumps = new()
        {
            // Escaleras
            { 3, 22 }, { 5, 8 }, { 11, 26 }, { 20, 29 },
            // Serpientes
            { 27, 1 }, { 21, 9 }, { 17, 4 }, { 19, 7 },
            // agrega las reales de tu tablero...
        };

        private const int FinalCell = 30;

        public async Task RollDiceAsync(int roomCode, int userId, CancellationToken ct = default)
        {
            var room = await _dbContext.Rooms
                .Include(r => r.RoomPlayers).ThenInclude(rp => rp.Player)
                .SingleOrDefaultAsync(r => r.Code == roomCode, ct)
                ?? throw new KeyNotFoundException("Sala no encontrada. ");
            if (room.IsStarted)
            {
                throw new InvalidOperationException("La partida ya fue iniciada. ");
            }
            if(room.RoomPlayers.Count < 2)
            {
                throw new InvalidOperationException("Se requieren al menos 2 jugadores.");
            }
            int order = 0; 
            foreach(var rp in room.RoomPlayers.OrderBy(rp => rp.Id))
            {
                rp.TurnOrder = order++;
                rp.Position = 0; 
            }
            room.CurrentTurnOrder = 0;
            room.IsStarted= true;

            await _dbContext.SaveChangesAsync(ct);

            await _hub.Clients.Group(room.Name).SendAsync("GameStarted",new
            {
                Players = room.RoomPlayers.Select(rp => new
                {
                    rp.Player,
                    rp.Player.Name,
                    rp.TurnOrder,
                    rp.Position
                }),
                firstTurnOrder = room.CurrentTurnOrder 
            }, ct);   
        }

        public async Task StartGameAsync(int roomCode, int userId, CancellationToken ct = default)
        {
            var sem = _roomLocks.GetOrAdd(roomCode, _ => new SemaphoreSlim(1,1));
            await sem.WaitAsync(ct);
            try
            {
                var room = await _dbContext.Rooms
                    .Include(r => r.RoomPlayers).ThenInclude(rp => rp.Player)
                    .SingleOrDefaultAsync(r=> r.Code == roomCode,ct)
                    ?? throw new KeyNotFoundException("sala no encontrada. ");

                if (!room.IsStarted)
                throw new NotImplementedException("La partida no está iniciada");

                // Identificar jugador asociado al userId
                var me = await _dbContext.Players.SingleOrDefaultAsync(p => p.Id == userId, ct)
                         ?? throw new InvalidOperationException("Jugador no existe.");

                var myRP = room.RoomPlayers.SingleOrDefault(rp => rp.PlayerId == me.Id)
                           ?? throw new InvalidOperationException("No estás en esta sala.");

                // Validar si es mi turno
                if (myRP.TurnOrder != room.CurrentTurnOrder)
                    throw new InvalidOperationException("No es tu turno.");
                // Tirar dado
                int dice = Random.Shared.Next(1, 7);


                int from = myRP.Position;
                int tentative = from + dice;

                if (tentative > FinalCell)
                {
                    // Regla típica: si te pasas, no avanzas o “rebote”.
                    // Aquí la simple: no avanzas.
                    tentative = from;
                }

                myRP.Position = tentative;

                // Aplicar serpiente/escalera
                if (_jumps.TryGetValue(myRP.Position, out var jumpTo))
                {
                    int pre = myRP.Position;
                    myRP.Position = jumpTo;
                    await _dbContext.SaveChangesAsync(ct);

                    await _hub.Clients.Group(room.Name).SendAsync("SnakesLaddersHit", new
                    {
                        playerId = me.Id,
                        from = pre,
                        to = myRP.Position
                    }, ct);
                }
                await _dbContext.SaveChangesAsync(ct);

                // Informar tirada y movimiento
                await _hub.Clients.Group(room.Name).SendAsync("DiceRolled", new
                {
                    playerId = me.Id,
                    dice,
                    from,
                    to = myRP.Position
                }, ct);

                // ¿Ganó?
                if (myRP.Position >= FinalCell)
                {
                    // Marcar victoria
                    _PalyerService.AddWin(me.Id);
                    await _dbContext.SaveChangesAsync(ct);

                    await _hub.Clients.Group(room.Name).SendAsync("PlayerWon", new
                    {
                        playerId = me.Id,
                        name = me.Name
                    }, ct);

                    // Opcional: cerrar sala o dejarla para otra ronda
                    room.IsStarted = false;
                    await _dbContext.SaveChangesAsync(ct);
                    return;
                }
                // Pasar turno
                int maxTurn = room.RoomPlayers.Max(rp => rp.TurnOrder);
                room.CurrentTurnOrder = (room.CurrentTurnOrder == maxTurn) ? 0 : room.CurrentTurnOrder + 1;

                await _dbContext.SaveChangesAsync(ct);

                await _hub.Clients.Group(room.Name).SendAsync("NextTurn", new
                {
                    turnOrder = room.CurrentTurnOrder
                }, ct);
            }
                finally
                {
                   sem.Release();
            }
        }
          
    }
 }

