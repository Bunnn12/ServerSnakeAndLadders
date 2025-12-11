using SnakeAndLadders.Contracts.Dtos;

namespace SnakesAndLadders.Data.Constants
{
    internal static class GameResultsConstants
    {
        public const int COMMAND_TIMEOUT_SECONDS = 30;
        public const int MIN_VALID_GAME_ID = 1;

        public const byte LOBBY_STATUS_CLOSED = (byte)LobbyStatus.Closed;
        public const byte WINNER_FLAG = 0x01;
        public const byte NOT_WINNER_FLAG = 0x00;

        public const string ERROR_GAME_ID_POSITIVE =
            "GameId must be positive.";

        public const string ERROR_GAME_NOT_FOUND =
            "Game not found.";

        public const string ERROR_DATABASE_FINALIZING_GAME =
            "Database error while finalizing game.";

        public const string ERROR_DATABASE_UPDATE_FINALIZING_GAME =
            "Database update error while finalizing game.";

        public const string ERROR_UNEXPECTED_FINALIZING_GAME =
            "Unexpected error while finalizing game.";

        public const string ERROR_FATAL_FINALIZING_GAME =
            "Unexpected fatal error while finalizing game.";

        public const string LOG_GAME_NOT_FOUND =
            "FinalizeGame: game not found. GameId={0}";

        public const string LOG_SQL_ERROR_FINALIZING_GAME =
            "SQL error while finalizing game.";

        public const string LOG_DB_UPDATE_ERROR_FINALIZING_GAME =
            "DbUpdate error while finalizing game.";

        public const string LOG_UNEXPECTED_ERROR_FINALIZING_GAME =
            "Unexpected error while finalizing game.";

        public const string LOG_FATAL_ERROR_FINALIZING_GAME =
            "Fatal error while creating DB context for FinalizeGame.";

        public const string LOG_SUCCESS_FINALIZING_GAME =
            "FinalizeGame: game closed successfully. GameId={0}, WinnerUserId={1}, RewardedUsers={2}";
    }
}
