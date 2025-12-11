using System;

namespace SnakesAndLadders.Services.Constants
{
    internal static class GameplayLogicConstants
    {
        internal const int INVALID_USER_ID = 0;
        internal const int MAX_CONSECUTIVE_TIMEOUTS = 3;

        internal const int MIN_CELL_INDEX = 0;
        internal const int MIN_BOARD_CELL = 1;

        internal const string TOKEN_SEPARATOR = "_";

        internal const string ERROR_GAME_ALREADY_FINISHED_EN =
            "The game has already finished.";

        internal const string ERROR_NO_PLAYERS_EN =
            "There are no players in this game.";

        internal const string ERROR_CURRENT_PLAYER_NOT_FOUND_EN =
            "Current player state was not found.";

        internal const string ERROR_USER_NOT_IN_GAME_EN =
            "User is not part of this game.";

        internal const string ERROR_NOT_USER_TURN_EN =
            "It is not this user's turn.";

        internal const string ERROR_PLAYER_STATE_NOT_FOUND_EN =
            "Player state was not found.";

        internal const string ERROR_GAME_ALREADY_FINISHED_ES =
            "La partida ya terminó.";

        internal const string ERROR_USER_NOT_IN_GAME_ES =
            "El usuario no forma parte de esta partida.";

        internal const string ERROR_NOT_USER_TURN_ES =
            "No es el turno de este jugador.";

        internal const string ERROR_PLAYER_FROZEN_ES =
            "El jugador está congelado y no puede usar ítems.";

        internal const string ERROR_PLAYER_ALREADY_ROLLED_ES =
            "El jugador ya tiró el dado en este turno.";

        internal const string ERROR_PLAYER_ITEM_ALREADY_USED_ES =
            "El jugador ya usó un ítem en este turno.";

        internal const string ROLL_TOO_HIGH_NO_MOVE = "RollTooHigh_NoMove";
        internal const string LADDER = "Ladder";
        internal const string SNAKE = "Snake";
        internal const string JUMP_SAME = "JumpButSameIndex";
        internal const string WIN = "Win";
        internal const string FROZEN_SKIP_TURN = "Frozen_SkipTurn";
        internal const string SNAKE_BLOCKED_BY_SHIELD = "Snake_BlockedByShield";
        internal const string ROCKET_USED = "Rocket_Used";
        internal const string ROCKET_IGNORED = "Rocket_Ignored";

    }

}
