using Escaleras_Serpientes.Dtos.Room;
using Escaleras_Serpientes.Entities;
using Escaleras_Serpientes.Services.Room;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Escaleras_Serpientes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
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
