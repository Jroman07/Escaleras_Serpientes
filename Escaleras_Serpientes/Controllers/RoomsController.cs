using Escaleras_Serpientes.Dtos.Room;
using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Hubs;
using Escaleras_Serpientes.Services.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Escaleras_Serpientes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IHubContext<GameHub> _hubContext;
        public RoomsController(IRoomService roomService, IHubContext<GameHub> hubContext)
        {
            _roomService = roomService;
            _hubContext = hubContext;
        }
        // GET: api/<RoomsController>
        [HttpGet]
        public IEnumerable<Room> Get()
        {
            return _roomService.GetAllRooms();
        }

        // GET api/<RoomsController>/5
        [HttpGet("{id}")]
        public Room Get(int id)
        {
            return _roomService.GetRoomById(id);
        }

        // POST api/<RoomsController>
        [HttpPost]
        public ActionResult<Room> Post([FromBody] CreateRoomDto dto)
        {
            var createdRoom = _roomService.CreateRoom(dto);
            if (createdRoom == null)
            {
                return Conflict("Room with the same name already exists or invalid data.");
            }
            return Ok(createdRoom);
        }

        [HttpPost("join")]
        [Authorize]
        public async Task<ActionResult<JoinRoomResponse>> Join([FromBody] JoinRoomRequest req, CancellationToken ct)
        {
            if (req.Code == 0) return BadRequest("Debe enviar el código de la sala.");

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var playerId))
                return Unauthorized("No fue posible identificar al usuario.");

            var result = await _roomService.JoinRoom(req.Code, playerId, ct);

            // grupo = código de sala (en string)
            var group = req.Code.ToString();

            // IMPORTANTE: manda un payload que tu front pueda leer y NO metas el CT como arg.
            await _hubContext.Clients.Group(group)
                .SendAsync("PlayerJoined",
                           new { group, player = "Servidor" },
                           cancellationToken: ct);

            return Ok(new JoinRoomResponse
            {
                RoomId = result.Id,
                RoomCode = result.Code,
                Group = result.Name,
                PlayersCount = result.RoomPlayers.Count,
                Capacity = result.MaxPlayers
            });
        }



        // PUT api/<RoomsController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<RoomsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _roomService.DeleteRoom(id);
        }
    }
}
