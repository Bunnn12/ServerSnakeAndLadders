using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class ChatAppService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatAppService));

        private const int DEFAULT_RECENT_MESSAGES_COUNT = 100;
        private const int MAX_MESSAGE_LENGTH = 500;
        private const int ZERO_MESSAGES_CAPACITY = 0;
        private const int MIN_VALID_STICKER_ID = 1;

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

            try
            {
                message.TimestampUtc = DateTime.UtcNow;
                message.Text = NormalizeMessageText(message.Text);
                message.SenderAvatarId = AvatarIdHelper.MapFromDb(message.SenderAvatarId);

                _chatRepository.SaveMessage(lobbyId, message);
            }
            catch (IOException ex)
            {
                _logger.Error("I/O error while saving chat message.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error("Unauthorized access while saving chat message.", ex);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while saving chat message.", ex);
            }
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            int effectiveTake = take <= 0
                ? DEFAULT_RECENT_MESSAGES_COUNT
                : take;

            IList<ChatMessageDto> messages;

            try
            {
                messages = _chatRepository.ReadLast(lobbyId, effectiveTake);
            }
            catch (IOException ex)
            {
                _logger.Error("I/O error while reading recent chat messages.", ex);
                return new List<ChatMessageDto>(ZERO_MESSAGES_CAPACITY);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error("Unauthorized access while reading recent chat messages.", ex);
                return new List<ChatMessageDto>(ZERO_MESSAGES_CAPACITY);
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while reading recent chat messages.", ex);
                return new List<ChatMessageDto>(ZERO_MESSAGES_CAPACITY);
            }

            if (messages == null)
            {
                return new List<ChatMessageDto>(ZERO_MESSAGES_CAPACITY);
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
            bool hasSticker = message.StickerId >= MIN_VALID_STICKER_ID
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
