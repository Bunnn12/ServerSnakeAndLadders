namespace SnakesAndLadders.Services.Constants
{
    internal static class ChatAppServiceConstants
    {
        public const int DEFAULT_RECENT_MESSAGES_COUNT = 100;
        public const int MAX_MESSAGE_LENGTH = 500;
        public const int ZERO_MESSAGES_CAPACITY = 0;
        public const int MIN_VALID_STICKER_ID = 1;

        public const string LOG_WARN_INVALID_MESSAGE =
            "Invalid chat message received. Message will be ignored.";

        public const string LOG_ERROR_SAVING_MESSAGE_IO =
            "I/O error while saving chat message.";

        public const string LOG_ERROR_SAVING_MESSAGE_UNAUTHORIZED =
            "Unauthorized access while saving chat message.";

        public const string LOG_ERROR_SAVING_MESSAGE_UNEXPECTED =
            "Unexpected error while saving chat message.";

        public const string LOG_ERROR_READING_MESSAGES_IO =
            "I/O error while reading recent chat messages.";

        public const string LOG_ERROR_READING_MESSAGES_UNAUTHORIZED =
            "Unauthorized access while reading recent chat messages.";

        public const string LOG_ERROR_READING_MESSAGES_UNEXPECTED =
            "Unexpected error while reading recent chat messages.";
    }
}
