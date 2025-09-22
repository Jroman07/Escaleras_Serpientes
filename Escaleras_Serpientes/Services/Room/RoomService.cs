using Escaleras_Serpientes.Dtos.Room;
using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Services.Player;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.EntityFrameworkCore;

namespace Escaleras_Serpientes.Services.Room
{
    public class RoomService : IRoomService
    {
        private readonly SnakesLaddersDbContext _dbContext;
        private readonly IPlayerService _playerService;
        public RoomService(SnakesLaddersDbContext dbContext, IPlayerService playerService)
        {
            _dbContext = dbContext;
            _playerService = playerService;
        }

        public Entities.Room CreateRoom(CreateRoomDto dto)
        {
            if (dto == null)
            {
                throw new Exception("Object is empty");
            }
            else
            {
                Entities.Room? Data = _dbContext.Rooms.Where(x => x.Name == dto.Name).FirstOrDefault();
                if (Data != null)
                {
                    return null;
                }
                else
                {
                    var room = dto.ToEntity();
                    while (true)
                    {
                        room.Code = new Random().Next(1000, 9999);
                        Entities.Room? codeExists = _dbContext.Rooms.Where(x => x.Code == room.Code).FirstOrDefault();
                        if (codeExists == null) break;
                    }
                    _dbContext.Rooms.Add(room);
                    _dbContext.SaveChanges();

                    return room;
                }

            }
        }

        public void DeleteRoom(int id)
        {
            Entities.Room DeleteRoom = _dbContext.Rooms.Find(id);
            if (DeleteRoom != null)
            {
                _dbContext.Rooms.Remove(DeleteRoom);

                _dbContext.SaveChanges();
            }
            else
            {
                throw new Exception("Room not found");
            }
        }

        public List<Entities.Room> GetAllRooms()
        {
            return _dbContext.Rooms.Include(x => x.RoomPlayers).ToList();
        }

        public Entities.Room GetRoomByCode(int code)
        {
            Entities.Room? room = _dbContext.Rooms
                .Include(x => x.RoomPlayers)
                .FirstOrDefault(r => r.Code == code);

            if (room == null)
            {
                return null;
            }
            return room;
        }

        public Entities.Room GetRoomById(int id)
        {
            Entities.Room? room = _dbContext.Rooms
                .Include(x => x.RoomPlayers)
                .FirstOrDefault(r => r.Id == id);

            if (room == null)
            {
                return null;
            }
            return room;
        }

        public async  Task<Entities.Room> JoinRoom(int codeRoom, int playerID, CancellationToken ct = default)
        {
            var room = await _dbContext.Rooms
            .Include(r => r.RoomPlayers)
            .SingleOrDefaultAsync(r => r.Code == codeRoom, ct)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

            if (room.IsStarted)
                throw new InvalidOperationException("La sala está cerrada.");

            // Buscar el jugador por UserId (mapeado del JWT)
            var player = await _dbContext.Players.SingleOrDefaultAsync(p => p.Id == playerID, ct)
                         ?? throw new InvalidOperationException("Jugador no existe para este usuario.");

            // Ya está dentro
            var alreadyIn = room.RoomPlayers.Any(rp => rp.PlayerId == player.Id);
            if (!alreadyIn)
            {
                // Capacidad
                if (room.RoomPlayers.Count >= room.MaxPlayers)
                    throw new InvalidOperationException("La sala está llena.");

                _dbContext.RoomPlayers.Add(new RoomPlayers
                {
                    RoomId = room.Id,
                    PlayerId = player.Id
                });

                await _dbContext.SaveChangesAsync(ct);
            }

            return room;
        }

        //public Entities.Room JoinRoom(code id)
        //{
        //    Entities.Room? room = _dbContext.Rooms
        //        .Include(x => x.Players)
        //        .FirstOrDefault(r => r.Id == id);

        //    if (room == null)
        //    {
        //        throw new Exception("Player not found");
        //    }
        //    return room;
        //}
    }
}
