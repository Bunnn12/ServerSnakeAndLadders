namespace SnakesAndLadders.Services.Constants
{
    internal static class LobbyServiceConstants
    {
        internal const string REASON_CLOSED = "CLOSED";
        internal const string REASON_KICKED = "KICKED";
        internal const string REASON_KICKED_BY_HOST = "KICKED_BY_HOST";

        internal const int CLEANUP_INTERVAL_SECONDS = 30;
        internal const int MIN_RANDOM_LOBBY_ID = 100_000;
        internal const int MAX_RANDOM_LOBBY_ID = 999_999;
        internal const int MIN_RANDOM_CODE = 0;
        internal const int MAX_RANDOM_CODE = 999_999;

        internal const int MIN_MAX_PLAYERS = 2;
        internal const int MAX_MAX_PLAYERS = 4;
        internal const int MIN_TTL_MINUTES = 5;

        internal const string DEFAULT_HOST_NAME_FORMAT = "User{0}";

        internal const string ERROR_REQ_NULL = "Solicitud nula.";
        internal const string ERROR_MAX_PLAYERS =
            "MaxPlayers debe estar entre 2 y 4.";
        internal const string ERROR_INVALID_CODE = "Código inválido.";
        internal const string ERROR_EXPIRED_OR_CLOSED =
            "La partida ha expirado o está cerrada.";
        internal const string ERROR_NOT_WAITING =
            "La partida ya comenzó o está cerrada.";
        internal const string ERROR_LOBBY_FULL = "El lobby está lleno.";
        internal const string ERROR_NOT_ENOUGH_PLAYERS =
            "Se requieren al menos 2 jugadores.";
        internal const string ERROR_ONLY_HOST_CAN_START =
            "Solo el host puede iniciar.";
        internal const string ERROR_LOBBY_NOT_FOUND =
            "Lobby no encontrado.";
        internal const string INFO_LEFT_LOBBY_ALREADY_CLOSED =
            "Lobby inexistente (ya cerrado).";
        internal const string INFO_NOT_IN_LOBBY =
            "No estaba en el lobby.";
        internal const string INFO_LOBBY_CLOSED = "Lobby cerrado.";
        internal const string INFO_LEFT_LOBBY = "Saliste del lobby.";
        internal const string INFO_MATCH_STARTING =
            "La partida se está iniciando...";

        internal const string ERROR_KICK_REQ_NULL =
            "Solicitud nula para expulsar jugador.";
        internal const string ERROR_KICK_REQ_INVALID =
            "Parámetros inválidos para expulsar jugador.";
        internal const string ERROR_KICK_SELF =
            "El host no puede expulsarse a sí mismo.";
        internal const string ERROR_KICK_NOT_HOST =
            "Solo el host puede expulsar jugadores del lobby.";
    }
}
