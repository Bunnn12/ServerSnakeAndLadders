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

        private readonly IChatRepository _chatRepository;

        public ChatAppService(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository
                              ?? throw new ArgumentNullException(nameof(chatRepository));
        }

        public void Send(int lobbyId, ChatMessageDto message)
        {
            if (!IsValidMessage(message))
            {
                return;
            }

            message.TimestampUtc = DateTime.UtcNow;
            message.Text = NormalizeMessageText(message.Text);
            message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);

            _chatRepository.SaveMessage(lobbyId, message);
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            int effectiveTake = take <= 0
                ? DEFAULT_RECENT_MESSAGES_COUNT
                : take;

            IList<ChatMessageDto> messages = _chatRepository.ReadLast(lobbyId, effectiveTake);

            if (messages == null)
            {
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

            bool hasText = !string.IsNullOrWhiteSpace(message.Text);
            bool hasSticker = message.StickerId > 0
                              && !string.IsNullOrWhiteSpace(message.StickerCode);

            if (!hasText && !hasSticker)
            {
                return false;
            }

            if (hasText && message.Text.Length > MAX_MESSAGE_LENGTH)
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
