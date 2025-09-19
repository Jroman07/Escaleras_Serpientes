using Escaleras_Serpientes.Dtos.Room;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.EntityFrameworkCore;

namespace Escaleras_Serpientes.Services.Room
{
    public class RoomService : IRoomService
    {
        private readonly SnakesLaddersDbContext _dbContext;
        public RoomService(SnakesLaddersDbContext dbContext)
        {
            _dbContext = dbContext;
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
            return _dbContext.Rooms.Include(x => x.Players).ToList();
        }

        public Entities.Room GetRoomById(int id)
        {
            Entities.Room? room = _dbContext.Rooms
                .Include(x => x.Players)
                .FirstOrDefault(r => r.Id == id);

            if (room == null)
            {
                throw new Exception("Player not found");
            }
            return room;
        }

        public Entities.Room JoinRoom(code id)
        {
            Entities.Room? room = _dbContext.Rooms
                .Include(x => x.Players)
                .FirstOrDefault(r => r.Id == id);

            if (room == null)
            {
                throw new Exception("Player not found");
            }
            return room;
        }
    }
}
