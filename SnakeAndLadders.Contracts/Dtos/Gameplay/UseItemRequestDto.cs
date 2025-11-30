
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class UseItemRequestDto
    {
        [DataMember(Order = 1)]
        public int GameId { get; set; }

        [DataMember(Order = 2)]
        public int PlayerUserId { get; set; }

        [DataMember(Order = 3)]
        public byte ItemSlotNumber { get; set; }

        [DataMember(Order = 4)]
        public int? TargetUserId { get; set; }
    }
}
