
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class ItemUsedNotificationDto
    {
        [DataMember(Order = 1)]
        public int GameId { get; set; }

        [DataMember(Order = 2)]
        public int UserId { get; set; }

        [DataMember(Order = 3)]
        public int? TargetUserId { get; set; }

        [DataMember(Order = 4)]
        public string ItemCode { get; set; }

        [DataMember(Order = 5)]
        public ItemEffectResultDto EffectResult { get; set; }

        [DataMember(Order = 6)]
        public GetGameStateResponseDto UpdatedGameState { get; set; }
    }
}
