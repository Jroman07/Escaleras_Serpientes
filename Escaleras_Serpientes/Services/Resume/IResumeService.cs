namespace Escaleras_Serpientes.Services.Resume
{
    public interface IResumeService
    {
        Task InitializeGameAsync(int roomCode, int startedByUserId, CancellationToken ct = default);
        Task PlayTurnAsync(int roomCode, int userId, CancellationToken ct = default);
    }
}
