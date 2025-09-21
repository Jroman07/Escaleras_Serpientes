using Escaleras_Serpientes.Dtos.Player;
using Escaleras_Serpientes.Entities;

namespace Escaleras_Serpientes.Services.Player
{
    public interface IPlayerService
    {
        public List<Entities.Player> GetAllPlayers();
        public List<WinsPlayerDto> GetRankigPlayers();
        public Entities.Player GetPlayerById(int id);
        public Payload CreatePlayer(CreatePlayerDto dto);
        public Entities.Player AddWin(int id);
        public Task<Entities.Player> FindMeAsync(int id, CancellationToken ct = default);
        public void DeletePlayer(int id);

    }
}
