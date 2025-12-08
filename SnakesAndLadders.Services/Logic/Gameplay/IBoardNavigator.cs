using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal interface IBoardNavigator
    {
        int FinalCellIndex { get; }

        int ApplyJumpEffectsIfAny(PlayerRuntimeState targetPlayer, int candidatePosition);

        SpecialCellResult ApplySpecialCellIfAny(
            PlayerRuntimeState playerState,
            int currentCellIndex,
            string currentExtraInfo);
    }
}
