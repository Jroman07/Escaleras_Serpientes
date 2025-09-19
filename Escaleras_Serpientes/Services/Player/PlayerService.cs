
using Escaleras_Serpientes.Dtos.Player;
using Escaleras_Serpientes.Services.Room;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.EntityFrameworkCore;

namespace Escaleras_Serpientes.Services.Player
{
    public class PlayerService : IPlayerService
    {
        private readonly SnakesLaddersDbContext _dbContext;
        private readonly RoomService _roomService;
        public PlayerService(SnakesLaddersDbContext dbContext, RoomService roomService)
        {
            _roomService = roomService;
            _dbContext = dbContext;
        }

        public Entities.Player AddWin(int id)
        {
            Entities.Player player = GetPlayerById(id);
            player.Wins++;
            _dbContext.SaveChanges();

            return player;
        }

        public Entities.Player CreatePlayer(CreatePlayerDto dto)
        {
            if (dto == null)
            {
                throw new Exception("Object is empty");
            }
            else
            {
                Entities.Player? Data = _dbContext.Players.Where(x => x.Name == dto.Name).FirstOrDefault();
                if (Data != null)
                {
                    return null;
                }
                else
                {
                    var player = dto.ToEntity();
                    _dbContext.Players.Add(player);
                    _dbContext.SaveChanges();

                    return player;
                }

            }
        }

        public void DeletePlayer(int Id)
        {
            Entities.Player DeletePlayer = _dbContext.Players.Find(Id);
            if (DeletePlayer != null)
            {
                _dbContext.Players.Remove(DeletePlayer);

                _dbContext.SaveChanges();
            }
            else
            {
                throw new Exception("Player not found");
            }
        }

        public List<Entities.Player> GetAllPlayers()
        {
            return _dbContext.Players.ToList();
        }

        public Entities.Player GetPlayerById(int id)
        {
            Entities.Player? player = _dbContext.Players
                .Include(x => x.ResumePlayers)
                .FirstOrDefault(c => c.Id == id);

            if (player == null)
            {
                throw new Exception("Player not found");
            }
            return player;
        }

        public List<WinsPlayerDto> GetRankigPlayers()
        {
            var query = _dbContext.Players
            .AsNoTracking()
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Name)
            .Select(p => new WinsPlayerDto { Name = p.Name, Wins = p.Wins });

            return query.ToList();
        }

        public Entities.Room JoinRoom(int code)
        {
            throw new NotImplementedException();
        }
    }
}
