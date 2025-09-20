using Escaleras_Serpientes.Services.Player;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Escaleras_Serpientes.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly PlayerService _playerService;
        public AuthService(PlayerService playerService)
        {
            _playerService = playerService;
        }
        private string GenerateJwtToken(int Id, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key_your_super_secret_key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "yourdomain.com",
                audience: "yourdomain.com",
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string Authenticate(int playerID)
        {
            if (playerID != 0)
            {
                Entities.Player? foundPlayer = _playerService.GetPlayerById(playerID);

                if (foundPlayer != null)
                {
                    var token = GenerateJwtToken(foundPlayer.Id, "Player");

                    return token;
                }
                else
                {
                    return null;
                }

            }
            return null;

        }
    }
}
