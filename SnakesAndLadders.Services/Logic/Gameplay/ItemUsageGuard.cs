using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal static class ItemUsageGuard
    {
        internal static void EnsurePlayerCanUseItem(PlayerRuntimeState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (playerState.RemainingFrozenTurns > 0)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_PLAYER_FROZEN_ES);
            }

            if (playerState.HasRolledThisTurn)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_PLAYER_ALREADY_ROLLED_ES);
            }

            if (playerState.ItemUsedThisTurn)
            {
                throw new InvalidOperationException(
                    GameplayLogicConstants.ERROR_PLAYER_ITEM_ALREADY_USED_ES);
            }
        }
    }
}
