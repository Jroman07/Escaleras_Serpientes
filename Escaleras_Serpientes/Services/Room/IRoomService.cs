namespace Escaleras_Serpientes.Services.Room
{
    public interface IRoomService
    {
        public List<Entities.Room> GetAllRooms();
        public Entities.Room GetRoomById(int id);
        public Entities.Room GetRoomByCode(int code);
        public Entities.Room CreateRoom(Dtos.Room.CreateRoomDto dto);
        Task<Entities.Room> DeletePlayerFromRoomAsync(int codeRoom, int playerId, CancellationToken ct = default);
        public Task<Entities.Room> JoinRoom(int codeRoom, int playerID, CancellationToken ct);
        public void DeleteRoom(int id);
    }
}
