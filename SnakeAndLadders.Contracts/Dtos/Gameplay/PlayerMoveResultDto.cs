using SnakeAndLadders.Contracts.Enums;
using System;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class PlayerMoveResultDto
    {
        public int UserId { get; set; }

        public int FromCellIndex { get; set; }

        public int ToCellIndex { get; set; }

        public int DiceValue { get; set; }

        public bool HasExtraTurn { get; set; }

        public bool HasWon { get; set; }

        public string Message { get; set; }

        public MoveEffectType EffectType { get; set; }

        public int? MessageIndex { get; set; }
    }
}
