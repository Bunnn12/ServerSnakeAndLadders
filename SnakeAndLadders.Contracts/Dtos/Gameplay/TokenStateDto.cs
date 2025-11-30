using System;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class TokenStateDto
    {
        public int UserId { get; set; }

        public int CellIndex { get; set; }

        public bool HasShield { get; set; }
        public int RemainingShieldTurns { get; set; }

        public int RemainingFrozenTurns { get; set; }

        public bool HasPendingRocketBonus { get; set; }
    }
}
