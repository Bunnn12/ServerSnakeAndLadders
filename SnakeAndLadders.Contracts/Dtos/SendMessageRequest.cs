using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public sealed class SendMessageRequest
    {
        [DataMember(IsRequired = true)] public ChatMessageDto Message { get; set; }
    }

    [DataContract]
    public sealed class SendMessageResponse
    {
        [DataMember(IsRequired = true)] public bool Ok { get; set; }
    }
}
