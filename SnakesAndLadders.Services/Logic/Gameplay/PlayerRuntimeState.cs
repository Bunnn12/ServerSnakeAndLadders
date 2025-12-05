using System;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal sealed class PlayerRuntimeState
    {
        public int UserId { get; set; }

        public int Position { get; set; }

        public int RemainingFrozenTurns { get; set; }

        public bool HasShield { get; set; }

        public int RemainingShieldTurns { get; set; }

        public int PendingRocketBonus { get; set; }

        public bool ItemUsedThisTurn { get; set; }

        public bool HasRolledThisTurn { get; set; }

        public int ConsecutiveTimeouts { get; set; }
    }
}
