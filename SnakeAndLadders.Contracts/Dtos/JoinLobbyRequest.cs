using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class JoinLobbyRequest
    {
        [DataMember] public string CodigoPartida { get; set; } 
        [DataMember] public int UserId { get; set; }
        [DataMember] public string UserName { get; set; }
    }
}
