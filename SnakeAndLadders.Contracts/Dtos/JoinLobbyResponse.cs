
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class JoinLobbyResponse
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string FailureReason { get; set; }
        [DataMember] public LobbyInfo Lobby { get; set; }
    }
}
