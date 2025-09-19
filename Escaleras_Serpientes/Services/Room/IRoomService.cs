namespace Escaleras_Serpientes.Services.Room
{
    public interface IRoomService
    {
        public List<Entities.Room> GetAllRooms();
        public Entities.Room GetRoomById(int id);
        public Entities.Room CreateRoom(Dtos.Room.CreateRoomDto dto);
        public void DeleteRoom(int id);
    }
}
