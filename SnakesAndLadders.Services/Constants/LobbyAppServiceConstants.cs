using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Constants
{
    internal static class LobbyAppServiceConstants
    {
        public const int DEFAULT_TTL_MINUTES = 30;
        public const int GAME_CODE_LENGTH = 6;
        public const int GAME_CODE_MAX_ATTEMPTS = 10;
        public const int GAME_CODE_MAX_VALUE_EXCLUSIVE = 1_000_000;
        public const int RANDOM_BYTES_LENGTH = 4;

        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 4;

        public const int MIN_VALID_USER_ID = 1;
        public const int MIN_VALID_LOBBY_ID = 1;

        public const string ERROR_REQUEST_NULL = "Request cannot be null.";
        public const string ERROR_HOST_USER_ID_POSITIVE = "HostUserId must be a positive number.";
        public const string ERROR_MAX_PLAYERS_RANGE = "MaxPlayers must be between 2 and 4.";
        public const string ERROR_FAILED_GENERATE_CODE = "Failed to generate a unique game code.";
        public const string ERROR_CONFLICT_CREATING_GAME = "A conflict occurred while creating the game. Please try again.";
        public const string ERROR_LOBBY_ID_POSITIVE = "lobbyId must be positive.";
        public const string ERROR_USER_ID_POSITIVE = "userId must be positive.";
        public const string ERROR_HOST_CANNOT_KICK_SELF = "Host cannot kick himself.";
        public const string ERROR_ONLY_HOST_CAN_KICK = "Only the host can kick players from the lobby.";

        public const string LOG_GAME_CREATED =
            "Game created: PartidaId={0}, Code={1}, ExpiresAt={2:u}";

        public const string LOG_PLAYER_REGISTERED =
            "Player registered in game. GameId={0}, UserId={1}, IsHost={2}";

        public const string LOG_CONFLICT_CREATING_GAME =
            "Conflict while creating game.";

        public const string LOG_FAILED_GENERATE_CODE =
            "Failed to generate a unique game code after {0} attempts.";
    }
}
