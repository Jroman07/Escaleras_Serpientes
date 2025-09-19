using System.Text.Json.Serialization;

namespace Escaleras_Serpientes.Entities
{
    public class ResumePlayer
    {
        [JsonIgnore]
        public int Id { get; set; }
        public int ResumeId { get; set; }
        [JsonIgnore]
        public Resume Resume { get; set; } = null!;

        public int PlayerId { get; set; }
        [JsonIgnore]
        public Player Player { get; set; } = null!;
    }
}
