namespace Escaleras_Serpientes.Services.Resume
{
    public interface IResumeService
    {
        Task StartGameAsync(int roomCode, int userId, CancellationToken ct = default);
        Task RollDiceAsync(int roomCode, int userId, CancellationToken ct = default);
    }
}
