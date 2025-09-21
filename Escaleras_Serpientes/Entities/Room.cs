using System.Text.Json.Serialization;

namespace Escaleras_Serpientes.Entities
{
    public class Room
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Code { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; } = 4; 
        public bool IsStarted { get; set; } = false;

        public int CurrentTurnOrder { get; set; } = 0;
        [JsonIgnore]
        public Resume? Resume { get; set; }
        [JsonIgnore]
        public List<RoomPlayers>? RoomPlayers { get; set; }
    }
}
