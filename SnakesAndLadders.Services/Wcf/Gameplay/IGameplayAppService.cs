using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    public interface IGameplayAppService
    {
        RollDiceResult RollDice(
            int gameId,
            int userId,
            string diceCode);

        GameStateSnapshot GetCurrentState(int gameId);

        ItemEffectResult UseItem(
            int gameId,
            int userId,
            string itemCode,
            int? targetUserId);

        TurnTimeoutResult HandleTurnTimeout(int gameId);
    }
}
