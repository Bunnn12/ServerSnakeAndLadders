using System;

namespace SnakesAndLadders.Services.Constants
{
    internal static class MatchInvitationConstants
    {
        internal const string ERROR_INVALID_REQUEST = "INVITE_INVALID_REQUEST";
        internal const string ERROR_INVALID_SESSION = "INVITE_INVALID_SESSION";
        internal const string ERROR_INVALID_GAME_CODE = "INVITE_INVALID_GAME_CODE";
        internal const string ERROR_INVALID_FRIEND_ID = "INVITE_INVALID_FRIEND_ID";
        internal const string ERROR_NOT_FRIENDS = "INVITE_NOT_FRIENDS";
        internal const string ERROR_FRIEND_EMAIL_NOT_FOUND =
            "INVITE_FRIEND_EMAIL_NOT_FOUND";
        internal const string ERROR_EMAIL_SEND_FAILED = "INVITE_EMAIL_SEND_FAILED";

        internal const int EXPECTED_GAME_CODE_LENGTH = 6;
        internal const int MIN_VALID_USER_ID = 1;

        internal const string LOG_GAME_INVITATION_SENT =
            "Game invitation sent. GameCode={0}, Inviter={1}, Friend={2}";

        internal const string LOG_ERROR_SENDING_INVITATION =
            "Error sending game invitation email.";

        internal const string FALLBACK_USERNAME_FORMAT = "User-{0}";
    }
}
