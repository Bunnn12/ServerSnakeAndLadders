
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos.Gameplay;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IGameSessionStore
    {
        GameSession CreateSession(
            int gameId,
            BoardDefinitionDto boardDefinition,
            IEnumerable<int> playerUserIds);

        bool TryGetSession(int gameId, out GameSession session);

        void UpdateSession(GameSession session);
    }
}
