namespace Escaleras_Serpientes.Services.Auth
{
    public interface IAuthService
    {
        public string GenerateJwtToken(int Id, string name);
    }
}
