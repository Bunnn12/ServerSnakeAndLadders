using System;

namespace SnakesAndLadders.Data.Constants
{
    internal static class SanctionRepositoryConstants
    {
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_USER_ID = 1;
        public const int DEFAULT_SANCTION_ID = 0;

        public const string DEFAULT_SANCTION_TYPE = "";

        public const string ERROR_DTO_REQUIRED = "SanctionDto is required.";
        public const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

        public const string LOG_ERROR_INSERT_SANCTION = "Error inserting sanction.";
        public const string LOG_ERROR_GET_LAST_SANCTION = "Error getting last sanction.";
        public const string LOG_ERROR_GET_SANCTIONS_HISTORY = "Error getting sanctions history.";
    }
}
