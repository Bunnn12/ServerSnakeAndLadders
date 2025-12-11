using SnakeAndLadders.Contracts.Dtos;

namespace SnakesAndLadders.Data.Constants
{
    public static class LobbyRepositoryConstants
    {
        public const byte LOBBY_STATUS_WAITING = 1;

        public const int MIN_VALID_GAME_ID = 1;
        public const int MIN_VALID_USER_ID = 1;

        public const string DEFAULT_DIFFICULTY = "Normal";

        public const string ERROR_GAME_ID_POSITIVE =
            "gameId must be positive.";

        public const string ERROR_REQUEST_NULL =
            "Request cannot be null.";

        public const string ERROR_USER_ID_POSITIVE =
            "userId must be positive.";

        public const string ERROR_CODE_REQUIRED =
            "Code is required.";

        public const string LOG_DB_ENTITY_VALIDATION_ERROR_CREATE_GAME =
            "Entity validation error while creating lobby game.";

        public const string LOG_DB_ENTITY_VALIDATION_DETAIL =
            "Validation error on {0}.{1}: {2}";
    }
}
