using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Constants
{
    internal static class UserRepositoryConstants
    {
        public const int MAX_USERNAME_LENGTH = 50;
        public const int MAX_FIRST_NAME_LENGTH = 100;
        public const int MAX_LAST_NAME_LENGTH = 255;
        public const int MAX_DESCRIPTION_LENGTH = 500;

        public const int MIN_VALID_USER_ID = 1;

        public const byte STATUS_ACTIVE = 0x01;
        public const int STATUS_MIN_LENGTH = 1;
        public const int STATUS_ACTIVE_INDEX = 0;

        public const int CURRENT_AVATAR_ENTITY_ID_NONE = 0;

        public const int COMMAND_TIMEOUT_SECONDS = 30;

        public const string ERROR_USERNAME_REQUIRED = "username es obligatorio.";
        public const string ERROR_USERNAME_MAX_LENGTH_TEMPLATE =
            "username excede la longitud máxima permitida ({0}).";

        public const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        public const string ERROR_FIRST_NAME_MAX_LENGTH_TEMPLATE =
            "FirstName exceeds {0} characters.";
        public const string ERROR_LAST_NAME_MAX_LENGTH_TEMPLATE =
            "LastName exceeds {0} characters.";
        public const string ERROR_PROFILE_DESCRIPTION_MAX_LENGTH_TEMPLATE =
            "ProfileDescription exceeds {0} characters.";

        public const string ERROR_USER_NOT_FOUND_OR_INACTIVE = "User not found or inactive.";

        public const string ERROR_AVATAR_CODE_REQUIRED = "AvatarCode is required.";
        public const string ERROR_AVATAR_CODE_UNKNOWN = "AvatarCode is not recognized.";
        public const string ERROR_AVATAR_NOT_UNLOCKED = "Avatar is not unlocked for the user.";
    }
}
