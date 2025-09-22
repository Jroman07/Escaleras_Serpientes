using System.Collections.Concurrent;

namespace Escaleras_Serpientes.Hubs
{
    // Implementación mínima para que el Hub funcione.
    public sealed class GameService : IGameService
    {
        private readonly ConcurrentDictionary<string, GameStateDto> _states = new();

        public Task<GameStateDto> ApplyActionAsync(
            string roomId, string playerName, ActionDetailsDto action, CancellationToken ct = default)
        {
            var state = _states.AddOrUpdate(
                roomId,
                _ => new GameStateDto(roomId, new { lastAction = action, by = playerName }, playerName),
                (_, s) => s with { State = new { lastAction = action, by = playerName }, CurrentTurnPlayer = playerName }
            );
            return Task.FromResult(state);
        }

        public Task<(string NextPlayerName, GameStateDto State)> AdvanceTurnAsync(
            string roomId, CancellationToken ct = default)
        {
            var current = _states.GetOrAdd(roomId, _ => new GameStateDto(roomId, new { }, "INIT"));
            var next = current with { CurrentTurnPlayer = "NEXT" };
            _states[roomId] = next;
            return Task.FromResult<(string, GameStateDto)>(("NEXT", next));
        }

        public Task<bool> IsGameEndedAsync(string roomId, out GameResultDto? results)
        {
            results = null; // demo: nunca termina
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<RankingEntryDto>> GetRankingAsync(CancellationToken ct = default)
        {
            IReadOnlyList<RankingEntryDto> ranking = Array.Empty<RankingEntryDto>();
            return Task.FromResult(ranking);
        }
    }
}
