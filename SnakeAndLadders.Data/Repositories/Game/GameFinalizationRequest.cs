using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Game
{
    public sealed class GameFinalizationRequest
    {
        public int GameId { get; }

        public int WinnerUserId { get; }

        public IDictionary<int, int> CoinsByUserId { get; }

        public GameFinalizationRequest(
            int gameId,
            int winnerUserId,
            IDictionary<int, int> coinsByUserId)
        {
            GameId = gameId;
            WinnerUserId = winnerUserId;
            CoinsByUserId = coinsByUserId
                ?? new Dictionary<int, int>();
        }
    }
}
