using SnakeAndLadders.Contracts.Enums;

namespace SnakesAndLadders.Data.Constants
{
    internal static class SocialProfileRepositoryConstants
    {
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_USER_ID = 1;
        public const int MAX_PROFILE_LINK_LENGTH = 255;

        public const string NETWORK_INSTAGRAM = "INSTAGRAM";
        public const string NETWORK_FACEBOOK = "FACEBOOK";
        public const string NETWORK_TWITTER = "TWITTER";

        public const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        public const string ERROR_REQUEST_NULL = "Request cannot be null.";
        public const string ERROR_PROFILE_LINK_REQUIRED = "ProfileLink is required.";
        public const string ERROR_SOCIAL_TYPE_REQUIRED = "TipoRedSocial is required.";
        public const string ERROR_UNKNOWN_SOCIAL_NETWORK_TEMPLATE = "Unknown social network type: {0}";

        public const string LOG_ERROR_GET_BY_USER = "Error getting social profiles for user.";
        public const string LOG_ERROR_UPSERT = "Error upserting social profile.";
        public const string LOG_ERROR_DELETE = "Error deleting social profile.";
    }
}
