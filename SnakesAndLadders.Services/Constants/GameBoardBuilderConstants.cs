using SnakeAndLadders.Contracts.Enums;

namespace SnakesAndLadders.Services.Constants
{
    public static class GameBoardBuilderConstants
    {
        public const int BOARD_SIZE_8_ROWS = 8;
        public const int BOARD_SIZE_8_COLUMNS = 8;

        public const int BOARD_SIZE_10_ROWS = 10;
        public const int BOARD_SIZE_10_COLUMNS = 10;

        public const int BOARD_SIZE_12_ROWS = 12;
        public const int BOARD_SIZE_12_COLUMNS = 12;

        public const int SPECIAL_CELLS_8_ONE_TYPE = 3;
        public const int SPECIAL_CELLS_8_TWO_TYPES = 4;
        public const int SPECIAL_CELLS_8_THREE_TYPES = 6;

        public const int SPECIAL_CELLS_10_ONE_TYPE = 4;
        public const int SPECIAL_CELLS_10_TWO_TYPES = 8;
        public const int SPECIAL_CELLS_10_THREE_TYPES = 12;

        public const int SPECIAL_CELLS_12_ONE_TYPE = 5;
        public const int SPECIAL_CELLS_12_TWO_TYPES = 12;
        public const int SPECIAL_CELLS_12_THREE_TYPES = 15;

        public const int COLOR_PATTERN_MODULO = 2;
        public const int MIN_CELL_INDEX = 1;

        public const int LADDERS_8 = 4;
        public const int LADDERS_10 = 5;
        public const int LADDERS_12 = 6;

        public const int SNAKES_8_EASY = 4;
        public const int SNAKES_8_MEDIUM = 5;
        public const int SNAKES_8_HARD = 6;

        public const int SNAKES_10_EASY = 5;
        public const int SNAKES_10_MEDIUM = 6;
        public const int SNAKES_10_HARD = 7;

        public const int SNAKES_12_EASY = 6;
        public const int SNAKES_12_MEDIUM = 7;
        public const int SNAKES_12_HARD = 8;

        public const int MAX_LADDER_PLACEMENT_ATTEMPTS = 1000;
        public const int MAX_SNAKE_PLACEMENT_ATTEMPTS = 1000;

        public const int ZONE_COUNT = 3;
        public const int MAX_ATTEMPTS_PER_SPECIAL_CELL = 100;

        public const int LADDERS_START_MIN_INDEX = 2;
        public const int LADDERS_START_MAX_OFFSET = 4;
        public const int LADDERS_MIN_DISTANCE = 2;

        public const int SNAKES_START_MIN_INDEX = 5;
        public const int SNAKES_END_MIN_INDEX = 2;
        public const int SNAKES_MIN_DISTANCE = 3;

        public const int DEFAULT_LADDERS_FALLBACK = 3;
        public const int DEFAULT_SNAKES_FALLBACK = 4;

        public const string DIFFICULTY_EASY = "easy";
        public const string DIFFICULTY_MEDIUM = "medium";
        public const string DIFFICULTY_HARD = "hard";

        public const string LOG_ERROR_NULL_REQUEST =
            "GameBoardBuilder.BuildBoard: request is null.";

        public const string LOG_ERROR_INVALID_GAME_ID =
            "GameBoardBuilder.BuildBoard: GameId must be greater than zero. Value={0}.";

        public const string LOG_INFO_BOARD_CREATED =
            "Board created successfully. Size={0}, Rows={1}, Columns={2}.";

        public const string LOG_INFO_SNAKES_LADDERS_ADDED =
            "Snakes and ladders added. Ladders={0}, Snakes={1}.";

        public const string LOG_WARN_NO_SPECIAL_CANDIDATES =
            "SpecialCellsAssigner.AssignSpecialCells: No candidate cells found for special cells.";

        public const string LOG_INFO_SPECIAL_CELLS_ASSIGNED =
            "Special cells assigned. Dice={0}, Item={1}, Message={2}.";
    }
}
