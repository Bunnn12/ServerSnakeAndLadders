// Contracts/Dtos/StartMatchRequest.cs
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class StartMatchRequest
    {
        [DataMember] public int PartidaId { get; set; }
        [DataMember] public int HostUserId { get; set; }
    }
}
