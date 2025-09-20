using Escaleras_Serpientes.Dtos.Player;
using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Hubs;
using Escaleras_Serpientes.Services.Player;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Escaleras_Serpientes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly IHubContext<GameHub> _hubContext;
        public PlayersController(IPlayerService playerService, IHubContext<GameHub> hubContext)
        {
            _playerService = playerService;
            _hubContext = hubContext;
        }
        // GET: api/<PlayersController>
        [HttpGet]
        public IEnumerable<Player> Get()
        {
            return _playerService.GetAllPlayers();
        }

        [HttpGet("ranking")]
        public IEnumerable<WinsPlayerDto> GetRanking()
        {
            return _playerService.GetRankigPlayers();
        }

        // GET api/<PlayersController>/5
        [HttpGet("{id}")]
        public Player Get(int id)
        {
            return _playerService.GetPlayerById(id);
        }

        // POST api/<PlayersController>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CreatePlayerDto dto)
        {
            var createdPlayer = _playerService.CreatePlayer(dto);
            if (createdPlayer == null)
            {
                return Conflict("Player with the same name already exists or invalid data.");
            }
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "New Player", createdPlayer.Player.Name, "Wins Number", createdPlayer.Player.Wins);
            return Ok(createdPlayer);
        }

        [HttpPost("signalR")]
        public async Task Post([FromBody] string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "PLAYER", message);
        }

        // PUT api/<PlayersController>/5
        [HttpPut("addWin/{id}")]
        public Player Put(int id)
        {
            return _playerService.AddWin(id);
        }

        // DELETE api/<PlayersController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _playerService.DeletePlayer(id);
        }
    }
}
