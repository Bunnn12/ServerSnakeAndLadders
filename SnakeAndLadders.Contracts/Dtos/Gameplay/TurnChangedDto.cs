using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class TurnChangedDto
    {
        [DataMember(Order = 1)]
        public int GameId { get; set; }

        [DataMember(Order = 2)]
        public int PreviousTurnUserId { get; set; }

        [DataMember(Order = 3)]
        public int CurrentTurnUserId { get; set; }

        [DataMember(Order = 4)]
        public bool IsExtraTurn { get; set; }

        [DataMember(Order = 5)]
        public string Reason { get; set; }
    }
}
