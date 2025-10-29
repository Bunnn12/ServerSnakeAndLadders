using System;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos
{
    [DataContract]
    public sealed class ChatMessageDto
    {
        [DataMember(IsRequired = true)] public string Sender { get; set; } = string.Empty;
        [DataMember(IsRequired = true)] public string Text { get; set; } = string.Empty;
        [DataMember(IsRequired = true)] public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        [DataMember(IsRequired = true)] public int SenderId { get; set; }
        [DataMember] public string SenderAvatarId { get; set; }

        public string Header => $"{Sender} · {TimestampUtc:HH:mm}";
    }
}
