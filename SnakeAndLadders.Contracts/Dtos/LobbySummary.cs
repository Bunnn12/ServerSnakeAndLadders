using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public sealed class LobbySummary
    {
        [DataMember] public int PartidaId { get; set; }
        [DataMember] public string CodigoPartida { get; set; }
        [DataMember] public string HostUserName { get; set; }
        [DataMember] public byte MaxPlayers { get; set; }
        [DataMember] public int CurrentPlayers { get; set; }
        [DataMember] public string Difficulty { get; set; }
        [DataMember] public bool IsPrivate { get; set; }
    }
}
