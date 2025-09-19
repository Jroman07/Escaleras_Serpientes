using Escaleras_Serpientes.Dtos.Player;
using System.ComponentModel.DataAnnotations;

namespace Escaleras_Serpientes.Dtos.Room
{
    public class CreateRoomDto
    {
        [Required, StringLength(80, MinimumLength = 2)]
        public string Name { get; set; } = null;
    }

    public static class RoomMappings
    {
        public static Entities.Room ToEntity(this CreateRoomDto dto) =>
            new Entities.Room
            {
                Name = dto.Name,
                MinPlayers = 2,        // valores por defecto
                MaxPlayers = 4,
                IsStarted = false,
            };
    }
}
