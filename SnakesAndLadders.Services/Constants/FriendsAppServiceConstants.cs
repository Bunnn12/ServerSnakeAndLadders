namespace SnakesAndLadders.Services.Constants
{
    internal static class FriendsAppServiceConstants
    {
        public const string CODE_INVALID_SESSION = "FRD_INVALID_SESSION";
        public const string CODE_SAME_USER = "FRD_SAME_USER";
        public const string CODE_LINK_EXISTS = "FRD_LINK_EXISTS";
        public const string CODE_LINK_NOT_FOUND = "FRD_LINK_NOT_FOUND";
        public const string CODE_NOT_IN_LINK = "FRD_NOT_IN_LINK";

        public const byte STATUS_ACCEPTED = 0x02;
        public const byte STATUS_REJECTED = 0x03;

        public const int DEFAULT_SEARCH_MAX_RESULTS = 50;

        public const string FAULT_MESSAGE_PENDING_EXISTS =
            "There is already a pending request.";

        public const string FAULT_MESSAGE_ALREADY_FRIENDS =
            "You are already friends.";

        public const string EX_MESSAGE_PENDING_ALREADY_EXISTS =
            "Pending already exists";

        public const string EX_MESSAGE_ALREADY_FRIENDS =
            "Already friends";

        public const string LOG_INFO_AUTO_ACCEPT_FORMAT =
            "Friend auto-accepted due to cross-request. users {0} <-> {1}";

        public const string LOG_INFO_REQUEST_CREATED_FORMAT =
            "Friend request created/reopened. users {0} -> {1}";

        public const string LOG_INFO_ACCEPTED_FORMAT =
            "Friend request accepted. linkId {0}";

        public const string LOG_INFO_REJECTED_FORMAT =
            "Friend request rejected. linkId {0}";

        public const string LOG_INFO_CANCELED_FORMAT =
            "Friend request canceled. linkId {0}";

        public const string LOG_INFO_REMOVED_FORMAT =
            "Friend removed. linkId {0}";
    }
}
