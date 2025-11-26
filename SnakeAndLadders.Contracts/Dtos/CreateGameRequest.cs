using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public sealed class CreateGameRequest
    {
        [DataMember] public int HostUserId { get; set; }
        [DataMember] public byte MaxPlayers { get; set; } = 2;
        [DataMember] public string Dificultad { get; set; }
        [DataMember] public int TtlMinutes { get; set; } = 30;
        [DataMember] public int BoardSide { get; set; }
        [DataMember] public byte PlayersRequested { get; set; }
        [DataMember] public string SpecialTiles { get; set; }
        [DataMember] public string HostAvatarId { get; set; }
        [DataMember] public int? CurrentSkinUnlockedId { get; set; }
        [DataMember] public string CurrentSkinId { get; set; }

        [DataMember] public bool IsPrivate { get; set; } = true;
    }
}
