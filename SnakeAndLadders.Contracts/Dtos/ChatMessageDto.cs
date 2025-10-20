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

        // Azúcar para el cliente (no es obligatorio serializarlo)
        public string Header => $"{Sender} · {TimestampUtc:HH:mm}";
    }
}
