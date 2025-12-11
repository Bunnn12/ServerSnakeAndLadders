using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;
using System;
using System.Security.Cryptography;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class LobbyAppService : ILobbyAppService
    {
        private readonly ILobbyRepository _lobbyRepository;
        private readonly IAppLogger _appLogger;

        public LobbyAppService(ILobbyRepository lobbyRepository, IAppLogger appLogger)
        {
            _lobbyRepository = lobbyRepository ?? throw new ArgumentNullException(nameof(lobbyRepository));
            _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            ValidateCreateGameRequest(request);

            int ttlMinutes = request.TtlMinutes <= 0
                ? LobbyAppServiceConstants.DEFAULT_TTL_MINUTES
                : request.TtlMinutes;

            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(ttlMinutes);
            string code = GenerateUniqueCode();

            var lobbyRequest = new CreateLobbyRequestDto
            {
                HostUserId = request.HostUserId,
                MaxPlayers = request.MaxPlayers,
                Difficulty = request.Dificultad,
                Code = code,
                ExpiresAtUtc = expiresAtUtc
            };

            try
            {
                CreatedGameInfo created = _lobbyRepository.CreateGame(lobbyRequest);

                string logMessage = string.Format(
                    LobbyAppServiceConstants.LOG_GAME_CREATED,
                    created.PartidaId,
                    created.Code,
                    created.ExpiresAtUtc);

                _appLogger.Info(logMessage);

                return new CreateGameResponse
                {
                    PartidaId = created.PartidaId,
                    CodigoPartida = created.Code,
                    ExpiresAtUtc = created.ExpiresAtUtc
                };
            }
            catch (InvalidOperationException ex)
            {
                string logMessage = LobbyAppServiceConstants.LOG_CONFLICT_CREATING_GAME;

                _appLogger.Error(logMessage, ex);

                throw new InvalidOperationException(
                    LobbyAppServiceConstants.ERROR_CONFLICT_CREATING_GAME,
                    ex);
            }
        }

        public void RegisterHostInGame(int gameId, int userId)
        {
            RegisterPlayerInternal(gameId, userId, true);
        }

        public void RegisterPlayerInGame(int gameId, int userId)
        {
            RegisterPlayerInternal(gameId, userId, false);
        }

        public void KickPlayerFromLobby(int lobbyId, int hostUserId, int targetUserId)
        {
            ValidateLobbyId(lobbyId, nameof(lobbyId));
            ValidateUserId(hostUserId, nameof(hostUserId));
            ValidateUserId(targetUserId, nameof(targetUserId));

            if (hostUserId == targetUserId)
            {
                throw new InvalidOperationException(
                    LobbyAppServiceConstants.ERROR_HOST_CANNOT_KICK_SELF);
            }

            bool hostIsValid = _lobbyRepository.IsUserHost(lobbyId, hostUserId);
            if (!hostIsValid)
            {
                throw new InvalidOperationException(
                    LobbyAppServiceConstants.ERROR_ONLY_HOST_CAN_KICK);
            }

            bool targetIsInLobby = _lobbyRepository.IsUserInLobby(lobbyId, targetUserId);
            if (!targetIsInLobby)
            {
                return;
            }

            _lobbyRepository.RemoveUserFromLobby(lobbyId, targetUserId);
        }

        private void RegisterPlayerInternal(int gameId, int userId, bool isHost)
        {
            ValidateLobbyId(gameId, nameof(gameId));
            ValidateUserId(userId, nameof(userId));

            _lobbyRepository.AddUserToGame(gameId, userId, isHost);

            string logMessage = string.Format(
                LobbyAppServiceConstants.LOG_PLAYER_REGISTERED,
                gameId,
                userId,
                isHost);

            _appLogger.Info(logMessage);
        }

        private static void ValidateCreateGameRequest(CreateGameRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    LobbyAppServiceConstants.ERROR_REQUEST_NULL);
            }

            if (request.HostUserId < LobbyAppServiceConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentException(
                    LobbyAppServiceConstants.ERROR_HOST_USER_ID_POSITIVE,
                    nameof(request.HostUserId));
            }

            if (request.MaxPlayers < LobbyAppServiceConstants.MIN_PLAYERS ||
                request.MaxPlayers > LobbyAppServiceConstants.MAX_PLAYERS)
            {
                throw new ArgumentException(
                    LobbyAppServiceConstants.ERROR_MAX_PLAYERS_RANGE,
                    nameof(request.MaxPlayers));
            }
        }

        private static void ValidateLobbyId(int lobbyId, string parameterName)
        {
            if (lobbyId < LobbyAppServiceConstants.MIN_VALID_LOBBY_ID)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    LobbyAppServiceConstants.ERROR_LOBBY_ID_POSITIVE);
            }
        }

        private static void ValidateUserId(int userId, string parameterName)
        {
            if (userId < LobbyAppServiceConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    LobbyAppServiceConstants.ERROR_USER_ID_POSITIVE);
            }
        }

        private string GenerateUniqueCode()
        {
            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                for (int attempt = 0; attempt < LobbyAppServiceConstants.GAME_CODE_MAX_ATTEMPTS; attempt++)
                {
                    byte[] bytes = new byte[LobbyAppServiceConstants.RANDOM_BYTES_LENGTH];
                    randomNumberGenerator.GetBytes(bytes);

                    int rawValue = BitConverter.ToInt32(bytes, 0) & int.MaxValue;

                    int value = rawValue % LobbyAppServiceConstants.GAME_CODE_MAX_VALUE_EXCLUSIVE;

                    string candidate = value.ToString("D" + LobbyAppServiceConstants.GAME_CODE_LENGTH);

                    if (!_lobbyRepository.CodeExists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            string logMessage = string.Format(
                LobbyAppServiceConstants.LOG_FAILED_GENERATE_CODE,
                LobbyAppServiceConstants.GAME_CODE_MAX_ATTEMPTS);

            _appLogger.Info(logMessage);

            throw new InvalidOperationException(
                LobbyAppServiceConstants.ERROR_FAILED_GENERATE_CODE);
        }
    }
}
