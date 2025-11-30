// SnakeAndLadders.Contracts/Dtos/Gameplay/ItemEffectResultDto.cs
using System.Runtime.Serialization;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class ItemEffectResultDto
    {
        [DataMember(Order = 1)]
        public string ItemCode { get; set; }

        [DataMember(Order = 2)]
        public ItemEffectType EffectType { get; set; }

        [DataMember(Order = 3)]
        public int UserId { get; set; }

        [DataMember(Order = 4)]
        public int? TargetUserId { get; set; }

        [DataMember(Order = 5)]
        public int? FromCellIndex { get; set; }

        [DataMember(Order = 6)]
        public int? ToCellIndex { get; set; }

        [DataMember(Order = 7)]
        public bool WasBlockedByShield { get; set; }

        [DataMember(Order = 8)]
        public bool TargetFrozen { get; set; }

        [DataMember(Order = 9)]
        public bool ShieldActivated { get; set; }
    }
}
