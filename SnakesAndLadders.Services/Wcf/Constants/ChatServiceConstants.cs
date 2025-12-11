namespace SnakesAndLadders.Services.Wcf.Constants
{
    internal static class ChatServiceConstants
    {
        public const int ZERO_MESSAGES_CAPACITY = 0;

        public const string LOG_ERROR_SEND_FAILED =
            "ChatService.SendMessage failed.";

        public const string LOG_ERROR_GET_RECENT_FAILED =
            "ChatService.GetRecent failed.";

        public const string LOG_WARN_NO_CALLBACK_CHANNEL =
            "Subscribe called but no callback channel is available. Subscription ignored.";

        public const string LOG_WARN_COMMUNICATION_ERROR_FORMAT =
            "Communication error sending chat message to user {0} in lobby {1}. Exception: {2}";

        public const string LOG_WARN_TIMEOUT_ERROR_FORMAT =
            "Timeout sending chat message to user {0} in lobby {1}. Exception: {2}";
    }
}
