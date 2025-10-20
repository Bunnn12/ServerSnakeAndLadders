
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class LeaveLobbyRequest
    {
        [DataMember] public int PartidaId { get; set; }
        [DataMember] public int UserId { get; set; }
    }
}
