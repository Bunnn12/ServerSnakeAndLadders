namespace SnakesAndLadders.Services.Wcf.Constants
{
    internal static class GameBoardServiceConstants
    {
        public const string ERROR_UNEXPECTED_CREATE =
            "An unexpected internal error occurred while creating the board.";

        public const string ERROR_UNEXPECTED_GET =
            "An unexpected error occurred while retrieving the board.";

        public const string ERROR_GAME_ID_INVALID =
            "GameId must be greater than zero.";

        public const string ERROR_REQUEST_NULL =
            "Request cannot be null.";

        public const string ERROR_SESSION_NOT_FOUND =
            "Game session not found.";

        public const string ERROR_NO_PLAYERS =
            "Cannot create a game session without players.";

        public const int INVALID_USER_ID = 0;

        public const string LOG_INFO_CREATING_BOARD_FORMAT =
            "Creating board. GameId={0}, BoardSize={1}, EnableDiceCells={2}, EnableItemCells={3}, EnableMessageCells={4}, Difficulty={5}";

        public const string LOG_WARN_NO_PLAYERS_FORMAT =
            "CreateBoard: no valid player IDs. GameId={0}, RawCount={1}";

        public const string LOG_INFO_SESSION_CREATED_FORMAT =
            "Game session created. GameId={0}, Players={1}";

        public const string LOG_WARN_SESSION_NOT_FOUND_FORMAT =
            "GetBoard: no session found for GameId {0}.";

        public const string LOG_WARN_VALIDATION_CREATE =
            "Validation error while creating board.";

        public const string LOG_WARN_CANNOT_CREATE_SESSION =
            "Cannot create game session.";

        public const string LOG_ERROR_UNEXPECTED_GET =
            "Unexpected error while retrieving board.";
    }
}
