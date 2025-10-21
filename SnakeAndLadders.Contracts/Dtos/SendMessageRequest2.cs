using System.Runtime.Serialization;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [DataContract]
    public sealed class SendMessageRequest2
    {
        [DataMember(IsRequired = true)] public int LobbyId { get; set; }
        [DataMember(IsRequired = true)] public ChatMessageDto Message { get; set; } = new ChatMessageDto();
        [DataMember(IsRequired = false)] public string AuthToken { get; set; } = string.Empty;
    }
}
