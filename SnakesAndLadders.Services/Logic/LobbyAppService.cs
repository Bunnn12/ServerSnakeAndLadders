using System;
using System.Security.Cryptography;
using System.Text;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class LobbyAppService : ILobbyAppService
    {
        private const int DEFAULT_TTL_MINUTES = 30;
        private const int GAME_CODE_LENGTH = 6;
        private const int GAME_CODE_MAX_ATTEMPTS = 10;
        private const int GAME_CODE_MAX_VALUE_EXCLUSIVE = 1_000_000;

        private readonly ILobbyRepository lobbyRepository;
        private readonly IAppLogger appLogger;


        public LobbyAppService(ILobbyRepository lobbyRepository, IAppLogger appLogger)
        {
            this.lobbyRepository = lobbyRepository ?? throw new ArgumentNullException(nameof(lobbyRepository));
            this.appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.HostUserId == 0)
            {
                throw new ArgumentException(
                    "HostUserId must be a positive number.",
                    nameof(request));
            }

            if (request.MaxPlayers < 2 || request.MaxPlayers > 4)
            {
                throw new ArgumentException(
                    "MaxPlayers must be between 2 and 4.",
                    nameof(request));
            }

            int ttlMinutes = request.TtlMinutes <= 0
                ? DEFAULT_TTL_MINUTES
                : request.TtlMinutes;

            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(ttlMinutes);

            string code = GenerateUniqueCode();

            try
            {
                CreatedGameInfo created = lobbyRepository.CreateGame(
                    request.HostUserId,
                    request.MaxPlayers,
                    request.Dificultad,
                    code,
                    expiresAtUtc);

                appLogger.Info(
                    string.Format(
                        "Game created: PartidaId={0}, Code={1}, ExpiresAt={2:u}",
                        created.PartidaId,
                        created.Code,
                        created.ExpiresAtUtc));

                return new CreateGameResponse
                {
                    PartidaId = created.PartidaId,
                    CodigoPartida = created.Code,
                    ExpiresAtUtc = created.ExpiresAtUtc
                };
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    "A conflict occurred while creating the game. Please try again.",
                    ex);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void RegisterPlayerInGame(int gameId, int userId, bool isHost)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            try
            {
                lobbyRepository.AddUserToGame(gameId, userId, isHost);
                appLogger.Info(
                    $"Player registered in game. GameId={gameId}, UserId={userId}, IsHost={isHost}");
            }
            catch (Exception )
            {
                throw;
            }
        }

        private string GenerateUniqueCode()
        {
            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                for (int attempt = 0; attempt < GAME_CODE_MAX_ATTEMPTS; attempt++)
                {
                    byte[] bytes = new byte[4];
                    randomNumberGenerator.GetBytes(bytes);

                    int rawValue = BitConverter.ToInt32(bytes, 0);
                    if (rawValue < 0)
                    {
                        rawValue = -rawValue;
                    }

                    int value = rawValue % GAME_CODE_MAX_VALUE_EXCLUSIVE;

                    string candidate = value.ToString("D6");

                    if (!lobbyRepository.CodeExists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            throw new InvalidOperationException("Failed to generate a unique game code.");
        }
    }
}
