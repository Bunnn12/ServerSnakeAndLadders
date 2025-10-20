// Contracts/Dtos/LobbyInfo.cs
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class LobbyInfo
    {
        [DataMember] public int PartidaId { get; set; }
        [DataMember] public string CodigoPartida { get; set; }
        [DataMember] public int HostUserId { get; set; }
        [DataMember] public string HostUserName { get; set; }
        [DataMember] public byte MaxPlayers { get; set; }
        [DataMember] public LobbyStatus Status { get; set; }
        [DataMember] public DateTime ExpiresAtUtc { get; set; }
        [DataMember] public List<LobbyMember> Players { get; set; } = new List<LobbyMember>();
    }
}
