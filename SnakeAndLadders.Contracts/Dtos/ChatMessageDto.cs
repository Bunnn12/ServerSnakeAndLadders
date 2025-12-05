using System;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class ChatMessageDto
    {
        public string Sender { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public int SenderId { get; set; }

        public string SenderAvatarId { get; set; } = string.Empty;
    }
}
