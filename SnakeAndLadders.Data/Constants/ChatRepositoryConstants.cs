using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Constants
{
    internal static class ChatRepositoryConstants
    {
        public const string APP_FOLDER_NAME = "SnakesAndLadders";
        public const string CHAT_FOLDER_NAME = "Chat";

        public const int DEFAULT_TAKE_MESSAGES = 50;
        public const int FILE_BUFFER_SIZE = 4096;
        public const int MIN_VALID_LOBBY_ID = 1;

        public const string ERROR_INVALID_LOBBY_ID =
            "LobbyId must be greater than or equal to {0}.";

        public const string ERROR_MESSAGE_REQUIRED =
            "Message must have text or a sticker.";

        public const string ERROR_DIRECTORY_TRAVERSAL =
            "Directory traversal attempt in FileChatRepository.";
    }
}
