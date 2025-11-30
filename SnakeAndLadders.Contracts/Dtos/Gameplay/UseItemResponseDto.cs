
using System.Runtime.Serialization;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class UseItemResponseDto
    {
        [DataMember(Order = 1)]
        public bool Success { get; set; }

        [DataMember(Order = 2)]
        public string FailureReason { get; set; }

        [DataMember(Order = 3)]
        public int GameId { get; set; }

        [DataMember(Order = 4)]
        public int PlayerUserId { get; set; }

        [DataMember(Order = 5)]
        public int? TargetUserId { get; set; }

        [DataMember(Order = 6)]
        public ItemEffectType EffectType { get; set; }

        [DataMember(Order = 7)]
        public string ItemCode { get; set; }

        [DataMember(Order = 8)]
        public GetGameStateResponseDto UpdatedGameState { get; set; }
    }
}
