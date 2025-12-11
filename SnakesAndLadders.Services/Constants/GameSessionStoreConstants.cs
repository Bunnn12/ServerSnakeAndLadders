namespace SnakesAndLadders.Services.Constants
{
    public static class GameSessionStoreConstants
    {
        public const int INVALID_USER_ID = 0;

        public const string ERROR_GAME_ID_POSITIVE =
            "GameId must be greater than zero.";

        public const string ERROR_BOARD_REQUIRED =
            "Board definition is required.";

        public const string ERROR_NO_PLAYERS =
            "Cannot create a game session without players.";
    }
}
