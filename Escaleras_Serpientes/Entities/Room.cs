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
        public int MaxPlayers { get; set; }
        public bool IsStarted { get; set; }
        [JsonIgnore]
        public List<Player> Players { get; set; }
        [JsonIgnore]
        public Resume? Resume { get; set; }
    }
}
