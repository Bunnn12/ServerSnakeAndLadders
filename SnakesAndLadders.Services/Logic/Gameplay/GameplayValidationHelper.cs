using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public static class GameplayValidationHelper
    {
        public static void ThrowGameAlreadyFinished()
        {
            throw new InvalidOperationException(
                GameplayLogicConstants.ERROR_GAME_ALREADY_FINISHED_EN);
        }

        public static void EnsureThereArePlayers(IList<int> turnOrder)
        {
            if (turnOrder == null || turnOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_NO_PLAYERS_EN);
            }
        }

        public static void EnsureUserInGame(
            IList<int> turnOrder,
            int userId)
        {
            if (turnOrder == null || !turnOrder.Contains(userId))
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_USER_NOT_IN_GAME_EN);
            }
        }

        public static void EnsureIsUserTurn(
            IList<int> turnOrder,
            int currentTurnIndex,
            int userId)
        {
            if (turnOrder == null || turnOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_NO_PLAYERS_EN);
            }

            int currentTurnUserId = turnOrder[currentTurnIndex];

            if (currentTurnUserId != userId)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_NOT_USER_TURN_EN);
            }
        }

        public static PlayerRuntimeState GetPlayerStateOrThrow(
            IDictionary<int, PlayerRuntimeState> playersByUserId,
            int userId)
        {
            if (playersByUserId == null ||
                !playersByUserId.TryGetValue(userId, out PlayerRuntimeState playerState))
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_PLAYER_STATE_NOT_FOUND_EN);
            }

            return playerState;
        }
    }
}
