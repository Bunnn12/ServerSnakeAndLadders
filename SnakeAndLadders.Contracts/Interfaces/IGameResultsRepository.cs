using System.Collections.Generic;
using ServerSnakesAndLadders.Common;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IGameResultsRepository
    {
        OperationResult<bool> FinalizeGame(
            int gameId,
            int winnerUserId,
            IDictionary<int, int> coinsByUserId);
    }
}
