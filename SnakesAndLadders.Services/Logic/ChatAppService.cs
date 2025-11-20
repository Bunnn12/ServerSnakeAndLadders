using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;

namespace SnakesAndLadders.Services.Logic
{
    /// <summary>
    /// Application service for lobby chat: sending and retrieving recent messages.
    /// </summary>
    public sealed class ChatAppService
    {
        private const int DEFAULT_RECENT_MESSAGES_COUNT = 100;

        private readonly IChatRepository _chatRepository;

        public ChatAppService(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository ?? throw new ArgumentNullException(nameof(chatRepository));
        }

        /// <summary>
        /// Sends a chat message to the specified lobby if it contains non-empty text.
        /// </summary>
        public void Send(int lobbyId, ChatMessageDto message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Text))
            {
                return;
            }

            message.TimestampUtc = DateTime.UtcNow;
            message.Text = message.Text.Trim();

            if (string.IsNullOrEmpty(message.Text))
            {
                return;
            }

            message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);

            _chatRepository.Append(lobbyId, message);
        }

        /// <summary>
        /// Gets the most recent messages for a lobby, up to the requested amount.
        /// </summary>
        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            int effectiveTake = take <= 0 ? DEFAULT_RECENT_MESSAGES_COUNT : take;

            IList<ChatMessageDto> messages = _chatRepository.ReadLast(lobbyId, effectiveTake);

            foreach (ChatMessageDto message in messages)
            {
                message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);
            }

            return messages;
        }
    }
}
