using Escaleras_Serpientes.Dtos.Player;
using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Services.Player;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Escaleras_Serpientes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
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
        public ActionResult<Player> Post([FromBody] CreatePlayerDto dto)
        {
            var createdPlayer = _playerService.CreatePlayer(dto);
            if (createdPlayer == null)
            {
                return Conflict("Player with the same name already exists or invalid data.");
            }
            return Ok(createdPlayer);
        }

        // PUT api/<PlayersController>/5
        [HttpPut("{id}")]
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
