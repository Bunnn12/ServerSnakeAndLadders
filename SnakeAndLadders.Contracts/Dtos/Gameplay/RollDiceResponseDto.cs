using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class RollDiceResponseDto
    {
        [DataMember(Order = 1)]
        public bool Success { get; set; }

        [DataMember(Order = 2)]
        public string FailureReason { get; set; }

        [DataMember(Order = 3)]
        public int PlayerUserId { get; set; }

        [DataMember(Order = 4)]
        public int FromCellIndex { get; set; }

        [DataMember(Order = 5)]
        public int ToCellIndex { get; set; }

        [DataMember(Order = 6)]
        public int DiceValue { get; set; }

        [DataMember(Order = 7)]
        public MoveEffectType MoveResult { get; set; }

        [DataMember(Order = 8)]
        public TokenStateDto[] UpdatedTokens { get; set; }

        [DataMember(Order = 9)]
        public string ExtraInfo { get; set; }

        [DataMember(Order = 10)]
        public int? MessageIndex { get; set; }

        [DataMember(Order = 11)]
        public string GrantedItemCode { get; set; }

        [DataMember(Order = 12)]
        public string GrantedDiceCode { get; set; }
    }
}
