using System;
using System.Collections.Generic;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class ChatAppService
    {
        private const int DEFAULT_RECENT_MESSAGES_COUNT = 100;
        private const int MAX_MESSAGE_LENGTH = 500;

        private readonly IChatRepository chatRepository;

        public ChatAppService(IChatRepository chatRepositoryValue)
        {
            chatRepository = chatRepositoryValue
                             ?? throw new ArgumentNullException(nameof(chatRepositoryValue));
        }

        public void Send(int lobbyId, ChatMessageDto message)
        {
            if (!IsValidMessage(message))
            {
                return;
            }

            message.TimestampUtc = DateTime.UtcNow;
            message.Text = NormalizeMessageText(message.Text);

            if (string.IsNullOrEmpty(message.Text))
            {
                return;
            }

            message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);

            chatRepository.SaveMessage(lobbyId, message);
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            int effectiveTake = take <= 0
                ? DEFAULT_RECENT_MESSAGES_COUNT
                : take;

            IList<ChatMessageDto> messages = chatRepository.ReadLast(lobbyId, effectiveTake);

            if (messages == null)
            {
                // Defensa extra, aunque repo ya nunca devuelve null.
                return new List<ChatMessageDto>(0);
            }

            foreach (ChatMessageDto message in messages)
            {
                message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);
            }

            return messages;
        }

        private static bool IsValidMessage(ChatMessageDto message)
        {
            if (message == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(message.Text))
            {
                return false;
            }

            return true;
        }

        private static string NormalizeMessageText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string trimmed = text.Trim();

            if (trimmed.Length > MAX_MESSAGE_LENGTH)
            {
                return trimmed.Substring(0, MAX_MESSAGE_LENGTH);
            }

            return trimmed;
        }
    }
}
