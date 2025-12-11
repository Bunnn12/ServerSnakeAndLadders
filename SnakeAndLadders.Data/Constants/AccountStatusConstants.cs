using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Constants
{
    public static class AccountStatusConstants
    {
        public const byte STATUS_ACTIVE_VALUE = 0x01;
        public const byte STATUS_INACTIVE_VALUE = 0x00;

        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_USER_ID = 1;
        public const int DELETED_USER_ID_PAD_LENGTH = 6;

        public const string DELETED_USERNAME_PREFIX = "deleted_";
        public const string DELETED_EMAIL_LOCAL_PART_PREFIX = "deleted+";
        public const string DELETED_EMAIL_DOMAIN = "invalid.local";

        public const string LOG_ERROR_UPDATING_ACTIVE_STATE =
            "Error updating active state for user. UserId={0}; IsActive={1}";
    }
}
