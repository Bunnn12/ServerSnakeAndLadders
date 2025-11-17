using System;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public class LobbyMember
    {
        [DataMember] public int UserId { get; set; }
        [DataMember] public string UserName { get; set; }
        [DataMember] public bool IsHost { get; set; }
        [DataMember] public DateTime JoinedAtUtc { get; set; }

        [DataMember] public string AvatarId { get; set; }

        [DataMember] public int? CurrentSkinUnlockedId { get; set; }
        [DataMember] public string CurrentSkinId { get; set; }
    }
}
