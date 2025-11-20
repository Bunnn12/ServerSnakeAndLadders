using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class PlayerLeftDto
    {
        [DataMember(Order = 1)]
        public int GameId { get; set; }

        [DataMember(Order = 2)]
        public int UserId { get; set; }

        [DataMember(Order = 3)]
        public string UserName { get; set; }

        [DataMember(Order = 4)]
        public bool WasCurrentTurnPlayer { get; set; }

        [DataMember(Order = 5)]
        public int? NewCurrentTurnUserId { get; set; }

        [DataMember(Order = 6)]
        public string Reason { get; set; }
    }
}
