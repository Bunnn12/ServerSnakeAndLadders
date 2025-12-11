using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class GameResultsHelper
    {
        public static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                GameResultsConstants.COMMAND_TIMEOUT_SECONDS;
        }

        public static byte[] BuildWinnerFlag(int winnerUserId, int currentUserId)
        {
            bool isWinner = winnerUserId > 0 && currentUserId == winnerUserId;

            return new[]
            {
                isWinner
                    ? GameResultsConstants.WINNER_FLAG
                    : GameResultsConstants.NOT_WINNER_FLAG
            };
        }

        public static void ApplyCoins(
            SnakeAndLaddersDBEntities1 context,
            IDictionary<int, int> coinsByUserId)
        {
            if (coinsByUserId == null || coinsByUserId.Count == 0)
            {
                return;
            }

            IList<int> userIds = coinsByUserId.Keys.ToList();

            IList<Usuario> users = context.Usuario
                .Where(user => userIds.Contains(user.IdUsuario))
                .ToList();

            foreach (Usuario user in users)
            {
                if (!coinsByUserId.TryGetValue(user.IdUsuario, out int coinsToAdd))
                {
                    continue;
                }

                checked
                {
                    user.Monedas += coinsToAdd;
                }
            }
        }
    }
}
