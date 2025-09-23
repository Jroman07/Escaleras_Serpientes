using Escaleras_Serpientes.Dtos.Player;
using Escaleras_Serpientes.Services.Auth;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.EntityFrameworkCore;

namespace Escaleras_Serpientes.Services.Player
{
    public class PlayerService : IPlayerService
    {
        private readonly SnakesLaddersDbContext _dbContext;
        private readonly IAuthService _authService;

        public PlayerService(SnakesLaddersDbContext dbContext, IAuthService authService)
        {
            _dbContext = dbContext;
            _authService = authService;
        }

        public Entities.Player AddWin(int id)
        {
            Entities.Player player = GetPlayerById(id);
            player.Wins++;
            _dbContext.SaveChanges();

            return player;
        }

        public Payload CreatePlayer(CreatePlayerDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), "El objeto DTO no puede ser nulo.");
            }

            // Verificar duplicados
            var existing = _dbContext.Players
                .FirstOrDefault(x => x.Name == dto.Name);

            if (existing != null)
            {
                throw new InvalidOperationException($"El nombre '{dto.Name}' ya está en uso. Elige otro.");
            }

            // Crear el nuevo jugador
            var player = dto.ToEntity();
            _dbContext.Players.Add(player);
            _dbContext.SaveChanges();

            var playerToken = _authService.GenerateJwtToken(player.Id, player.Name);

            return new Payload
            {
                token = playerToken,
                Player = player
            };
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

        public async Task<Entities.Player> FindMeAsync(int id, CancellationToken ct = default)
        {
            var player = await _dbContext.Players
                .SingleOrDefaultAsync(p => p.Id == id, ct);

            return player ?? throw new KeyNotFoundException("Player not found");
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
    }
}
