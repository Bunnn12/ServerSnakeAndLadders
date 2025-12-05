using System.Runtime.Serialization;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    public sealed class SendMessageRequest2
    {
        public int LobbyId { get; set; }

        public ChatMessageDto Message { get; set; } = new ChatMessageDto();

        public string AuthToken { get; set; } = string.Empty;
    }
}
