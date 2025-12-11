using System;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class LobbyRepositoryHelper
    {
        public static string NormalizeDifficulty(string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                return LobbyRepositoryConstants.DEFAULT_DIFFICULTY;
            }

            return difficulty.Trim();
        }

        public static void ValidateGameId(int gameId)
        {
            if (gameId < LobbyRepositoryConstants.MIN_VALID_GAME_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId), LobbyRepositoryConstants.ERROR_GAME_ID_POSITIVE);
            }
        }

        public static void ValidateUserId(int userId)
        {
            if (userId < LobbyRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), LobbyRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }
        }

        public static UsuarioHasPartida CreateUserGameLink(
            int gameId,
            int userId,
            bool isHost)
        {
            return new UsuarioHasPartida
            {
                UsuarioIdUsuario = userId,
                PartidaIdPartida = gameId,
                esHost = isHost,
                Ganador = null
            };
        }
    }
}
