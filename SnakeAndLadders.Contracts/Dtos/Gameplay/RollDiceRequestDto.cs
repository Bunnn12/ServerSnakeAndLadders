using SnakeAndLadders.Contracts.Enums;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class RollDiceRequestDto
    {
        [DataMember(Order = 1)]
        public int GameId { get; set; }

        [DataMember(Order = 2)]
        public int PlayerUserId { get; set; }

        [DataMember(Order = 3)]
        public int CurrentCellIndex { get; set; }
    }

    
}
