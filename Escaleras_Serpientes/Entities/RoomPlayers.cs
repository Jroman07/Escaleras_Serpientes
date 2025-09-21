using System.Text.Json.Serialization;

namespace Escaleras_Serpientes.Entities
{
    public class RoomPlayers
    {
        [JsonIgnore]
        public int Id { get; set; }

        public int PlayerId { get; set; }

        [JsonIgnore]
        public Player Player { get; set; } = null;

        public int RoomId {get; set;}

        [JsonIgnore]
        public Room Room { get; set; } = null; 

    }
}
