using System.Text.Json.Serialization;

namespace Escaleras_Serpientes.Entities
{
    public class Player
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public int Wins { get; set; }
        [JsonIgnore]
        public List<ResumePlayer>? ResumePlayers { get; set; }
        [JsonIgnore]
        public List<RoomPlayers>? RoomPlayers { get; set; }
    }
}
