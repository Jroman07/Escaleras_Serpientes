using System.Text.Json.Serialization;

namespace Escaleras_Serpientes.Entities
{
    public class Resume
    {
        [JsonIgnore]
        public int Id { get; set; }
        public DateTime Date { get; set; }

        // FK para 1:1 con Room (Room es el principal)
        public int RoomId { get; set; }
        [JsonIgnore]
        public Room Room { get; set; } = null!;
        [JsonIgnore]
        public List<ResumePlayer>? ResumePlayers { get; set; }
    }
}
