using Escaleras_Serpientes.Services.Resume;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Escaleras_Serpientes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResumeController : ControllerBase
    {
        private readonly IResumeService _resumeService;

        public ResumeController(IResumeService resumeService)
        {
            _resumeService = resumeService;
        }

        // POST api/RoomsGame/init/1234

        [HttpPost("init/{roomCode}")]
        [Authorize]
        public async Task<ActionResult> InitializeGame(int roomCode, CancellationToken ct)
        {
            var userId = GetUserIdFromClaims();
            if (userId is null)
                return Unauthorized("No fue posible identificar al usuario.");

            try
            {
                await _resumeService.RollDiceAsync(roomCode, userId.Value, ct);
                return Ok("Partida inicializada.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno al iniciar la partida.");
            }
        }

        // POST api/RoomsGame/turn/1234
        [HttpPost("turn/{roomCode}")]
        public async Task<ActionResult> PlayTurn(int roomCode, CancellationToken ct)
        {
            var userId = GetUserIdFromClaims();
            if (userId is null)
                return Unauthorized("No fue posible identificar al usuario.");

            try
            {
                await _resumeService.StartGameAsync(roomCode, userId.Value, ct);
                return Ok("Turno procesado.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno al procesar el turno.");
            }
        }

        // Helper para leer el ID del JWT
        private int? GetUserIdFromClaims()
        {
            var idClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(idClaim, out var uid) ? uid : null;
        }
    }
}
