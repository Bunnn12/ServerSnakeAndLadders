using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Constants
{
    internal static class AccountsRepositoryConstants
    {
        public const int EMAIL_MAX_LENGTH = 200;
        public const int USERNAME_MAX_LENGTH = 90;
        public const int PROFILE_DESC_MAX_LENGTH = 510;
        public const int PASSWORD_HASH_MAX_LENGTH = 510;
        public const int PROFILE_PHOTO_ID_MAX_LENGTH = 5;
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int INITIAL_COINS = 0;
        public const byte STATUS_ACTIVE = 1;

        public const int MIN_VALID_USER_ID = 1;
        public const int MIN_PASSWORD_HISTORY_COUNT = 1;
        public const int STATUS_MIN_LENGTH = 1;
        public const int STATUS_ACTIVE_INDEX = 0;

        public const int EMPTY_COLLECTION_COUNT = 0;
        public const int MULTIPLE_ITEMS_MIN_COUNT = 2;
        public const int SUBSTRING_START_INDEX = 0;

        public const string ERROR_REQUEST_NULL = "Request cannot be null.";
        public const string ERROR_IDENTIFIER_REQUIRED = "Identifier is required.";
        public const string ERROR_INVALID_USERNAME_OR_PASSWORD = "Invalid username or password.";
        public const string ERROR_INVALID_CREDENTIALS = "Invalid credentials.";
        public const string ERROR_PROFILE_DATA_NOT_FOUND = "Profile data not found.";
        public const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        public const string ERROR_MAX_COUNT_POSITIVE = "maxCount must be positive.";
        public const string ERROR_PASSWORD_HASH_REQUIRED = "PasswordHash is required.";
        public const string ERROR_ACTIVE_ACCOUNT_NOT_FOUND = "Active account not found for user.";
        public const string ERROR_DATABASE_REGISTERING_USER = "Database error while registering user.";
        public const string ERROR_UNEXPECTED_REGISTERING_USER = "Unexpected error while registering user.";
        public const string ERROR_DATABASE_LOADING_PASSWORD_HISTORY = "Database error while loading password history.";
        public const string ERROR_UNEXPECTED_LOADING_PASSWORD_HISTORY = "Unexpected error while loading password history.";
        public const string ERROR_DATABASE_UPDATING_PASSWORD = "Database error while updating password.";
        public const string ERROR_UNEXPECTED_UPDATING_PASSWORD = "Unexpected error while updating password.";
        public const string ERROR_DATABASE_LOADING_EMAIL = "Database error while loading email.";
        public const string ERROR_UNEXPECTED_LOADING_EMAIL = "Unexpected error while loading email.";
        public const string ERROR_REQUIRED_TEMPLATE = "{0} is required.";

        public const string LOG_SQL_ERROR_CREATE_USER = "SQL error while creating user.";
        public const string LOG_UNEXPECTED_ERROR_CREATE_USER = "Unexpected error while creating user.";
        public const string LOG_INTEGRITY_USER_WITHOUT_PASSWORD = "Integrity issue: user {0} exists but has no password.";
        public const string LOG_SQL_ERROR_PASSWORD_HISTORY = "SQL error while loading password history.";
        public const string LOG_UNEXPECTED_ERROR_PASSWORD_HISTORY = "Unexpected error while loading password history.";
        public const string LOG_SQL_ERROR_INSERT_PASSWORD = "SQL error while inserting new password.";
        public const string LOG_UNEXPECTED_ERROR_INSERT_PASSWORD = "Unexpected error while inserting new password.";
        public const string LOG_SQL_ERROR_EMAIL_BY_USER_ID = "SQL error while loading email by user id.";
        public const string LOG_UNEXPECTED_ERROR_EMAIL_BY_USER_ID = "Unexpected error while loading email by user id.";
        public const string LOG_NO_ACTIVE_ACCOUNT_FOR_USER_CHANGING_PASSWORD = "No active account found for user when changing password. UserId={0}";
        public const string LOG_NO_ACTIVE_ACCOUNT_FOR_USER_GET_EMAIL = "GetEmailByUserId: no active account found for user. UserId={0}";
        public const string LOG_MULTIPLE_ACTIVE_ACCOUNTS_EMAIL = "Found {0} ACTIVE accounts with the same email {1}. Using the one with highest IdCuenta.";
        public const string LOG_MULTIPLE_ACTIVE_USERS_USERNAME = "Found {0} ACTIVE users with the same NombreUsuario {1}. Using the one with highest IdUsuario.";
        public const string LOG_MULTIPLE_ACTIVE_ACCOUNTS_FOR_USER = "Found {0} ACTIVE accounts for user {1}. Using the one with highest IdCuenta.";
    }
}
