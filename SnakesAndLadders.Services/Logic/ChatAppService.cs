using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class ChatAppService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatAppService));

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
                _logger.Warn(ChatAppServiceConstants.LOG_WARN_INVALID_MESSAGE);
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
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_SAVING_MESSAGE_IO, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_SAVING_MESSAGE_UNAUTHORIZED, ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_SAVING_MESSAGE_UNEXPECTED, ex);
            }
        }

        public IList<ChatMessageDto> GetRecent(int lobbyId, int take)
        {
            int effectiveTake = take <= 0
                ? ChatAppServiceConstants.DEFAULT_RECENT_MESSAGES_COUNT
                : take;

            IList<ChatMessageDto> messages;

            try
            {
                messages = _chatRepository.ReadLast(lobbyId, effectiveTake);
            }
            catch (IOException ex)
            {
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_READING_MESSAGES_IO, ex);
                return CreateEmptyMessages();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_READING_MESSAGES_UNAUTHORIZED, ex);
                return CreateEmptyMessages();
            }
            catch (Exception ex)
            {
                _logger.Error(ChatAppServiceConstants.LOG_ERROR_READING_MESSAGES_UNEXPECTED, ex);
                return CreateEmptyMessages();
            }

            if (messages == null)
            {
                return CreateEmptyMessages();
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
            bool hasSticker = message.StickerId >= ChatAppServiceConstants.MIN_VALID_STICKER_ID
                              && !string.IsNullOrWhiteSpace(message.StickerCode);

            if (!hasText && !hasSticker)
            {
                return false;
            }

            if (hasText && message.Text.Length > ChatAppServiceConstants.MAX_MESSAGE_LENGTH)
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

            if (trimmed.Length > ChatAppServiceConstants.MAX_MESSAGE_LENGTH)
            {
                return trimmed.Substring(0, ChatAppServiceConstants.MAX_MESSAGE_LENGTH);
            }

            return trimmed;
        }

        private static IList<ChatMessageDto> CreateEmptyMessages()
        {
            return new List<ChatMessageDto>(ChatAppServiceConstants.ZERO_MESSAGES_CAPACITY);
        }
    }
}
