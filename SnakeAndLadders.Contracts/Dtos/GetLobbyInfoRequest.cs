// Contracts/Dtos/GetLobbyInfoRequest.cs
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class GetLobbyInfoRequest
    {
        [DataMember] public int PartidaId { get; set; }
    }
}
