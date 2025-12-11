using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class ChatMessageValidator
    {
        public static void ValidateLobbyId(int lobbyId)
        {
            if (lobbyId < ChatRepositoryConstants.MIN_VALID_LOBBY_ID)
            {
                string message = string.Format(
                    ChatRepositoryConstants.ERROR_INVALID_LOBBY_ID,
                    ChatRepositoryConstants.MIN_VALID_LOBBY_ID);

                throw new ArgumentOutOfRangeException(nameof(lobbyId), message);
            }
        }

        public static void ValidateMessage(ChatMessageDto message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            bool hasText = !string.IsNullOrWhiteSpace(message.Text);
            bool hasSticker = message.StickerId > 0
                              && !string.IsNullOrWhiteSpace(message.StickerCode);

            if (!hasText && !hasSticker)
            {
                throw new ArgumentException(
                    ChatRepositoryConstants.ERROR_MESSAGE_REQUIRED,
                    nameof(message));
            }
        }
    }
}
