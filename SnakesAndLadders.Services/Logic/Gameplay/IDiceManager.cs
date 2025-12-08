using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal interface IDiceManager
    {
        int GetDiceValue(PlayerRuntimeState playerState, string diceCode);
    }
}
