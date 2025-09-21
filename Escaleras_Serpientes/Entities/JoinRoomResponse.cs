namespace Escaleras_Serpientes.Entities
{
    public class JoinRoomResponse
    {
        public int RoomId { get; set; }
        public int RoomCode { get; set; } = default!;
        public string Group { get; set; } = default!; // nombre del grupo SignalR (p.ej. code)
        public int PlayersCount { get; set; }
        public int Capacity { get; set; }
    }

    public class JoinRoomRequest
    {
        public int Code { get; set; } = default!;
    }
}
