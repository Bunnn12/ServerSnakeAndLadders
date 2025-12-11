using System;

namespace SnakesAndLadders.Data.Constants
{
    internal static class StatsRepositoryConstants
    {
        public const int MAX_ALLOWED_RESULTS = 100;
        public const int DEFAULT_RANKING_MAX_RESULTS = 50;
        public const int MIN_RESULTS = 1;
        public const int MIN_VALID_USER_ID = 1;

        public const int INDEX_NOT_FOUND = -1;
        public const int RANKING_POSITION_NOT_AVAILABLE = 0;

        public const int WINNER_FLAG_MIN_LENGTH = 1;
        public const int WINNER_FLAG_INDEX = 0;
        public const byte WINNER_FLAG = 0x01;

        public const int COMMAND_TIMEOUT_SECONDS = 30;

        public const decimal DEFAULT_WIN_PERCENTAGE = 0m;
        public const decimal WIN_PERCENTAGE_FACTOR = 100m;
        public const int WIN_PERCENTAGE_DECIMALS = 2;

        public const int DEFAULT_COINS = 0;
        public const int DEFAULT_MATCHES_PLAYED = 0;
        public const int DEFAULT_MATCHES_WON = 0;

        public const string ERROR_MAX_RESULTS_POSITIVE = "maxResults must be greater than zero.";
        public const string ERROR_USER_ID_POSITIVE = "userId must be positive.";
        public const string ERROR_RANKING_MAX_RESULTS_POSITIVE = "rankingMaxResults must be greater than zero.";

        public const string LOG_ERROR_GET_TOP_PLAYERS_BY_COINS = "Error getting top players by coins.";
        public const string LOG_ERROR_GET_PLAYER_STATS = "Error getting player stats.";
    }
}
