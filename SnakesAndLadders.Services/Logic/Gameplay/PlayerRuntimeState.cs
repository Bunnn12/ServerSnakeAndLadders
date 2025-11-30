using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal class PlayerRuntimeState
    {
        public int UserId { get; set; }

        public int Position { get; set; }

        public int RemainingFrozenTurns { get; set; }

        public bool HasShield { get; set; }

        public int RemainingShieldTurns { get; set; }

        public int PendingRocketBonus { get; set; }

        public bool ItemUsedThisTurn { get; set; }

        public bool HasRolledThisTurn { get; set; }
    }
}
