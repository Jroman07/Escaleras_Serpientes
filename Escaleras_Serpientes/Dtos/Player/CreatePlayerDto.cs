using Escaleras_Serpientes.Entities;
using System.ComponentModel.DataAnnotations;

namespace Escaleras_Serpientes.Dtos.Player
{
    public class CreatePlayerDto
    {
        [Required, StringLength(80, MinimumLength = 2)]
        public string Name { get; set; } = null;
    }

    public static class PlayerMappings
    {
        public static Entities.Player ToEntity(this CreatePlayerDto dto) =>
            new Entities.Player
            {
                Name = dto.Name,
                Wins = 0,        // valores por defecto
                TurnOrder = null,
                Position = null,
                RoomId = null
            };
    }
}
