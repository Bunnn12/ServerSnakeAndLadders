using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal interface IItemEffectManager
    {
        ItemEffectResult UseItem(
            string itemCode,
            int userId,
            int? targetUserId,
            IDictionary<int, PlayerRuntimeState> playersByUserId,
            Func<PlayerRuntimeState, int, int> applyJumpEffects);
    }
}
