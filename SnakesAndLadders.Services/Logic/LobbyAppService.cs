using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System.Data.Sql;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class LobbyAppService : ILobbyAppService
    {
        private readonly ILobbyRepository repo;
        private readonly IAppLogger log;

        public LobbyAppService(ILobbyRepository repo, IAppLogger log)
        {
            this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public CreateGameResponse CreateGame(CreateGameRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.HostUserId <= 0)
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

            var ttl = TimeSpan.FromMinutes(request.TtlMinutes <= 0 ? 30 : request.TtlMinutes);
            var expiresAt = DateTime.UtcNow.Add(ttl);
            var code = GenerateUniqueCode(6);

            try
            {
                var created = repo.CreateGame(request.HostUserId, request.MaxPlayers, request.Dificultad, code, expiresAt);

                log.Info($"Game created: PartidaId={created.PartidaId}, Code={created.Code}, ExpiresAt={created.ExpiresAtUtc:u}");

                return new CreateGameResponse
                {
                    PartidaId = created.PartidaId,
                    CodigoPartida = created.Code,
                    ExpiresAtUtc = created.ExpiresAtUtc
                };
            }
            catch (InvalidOperationException ex)
            {
                log.Error("DbUpdateException while creating game.", ex);
                throw new InvalidOperationException("A conflict occurred while creating the game. Please try again.", ex);
            }
            catch (Exception ex)
            {
                log.Error("Unexpected error while creating game.", ex);
                throw;
            }
        }

        private string GenerateUniqueCode(int length, int maxAttempts = 10)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var bytes = new byte[length];
                    rng.GetBytes(bytes);

                    var sb = new StringBuilder(length);
                    for (int i = 0; i < length; i++)
                        sb.Append(alphabet[bytes[i] % alphabet.Length]);

                    var candidate = sb.ToString();
                    if (!repo.CodeExists(candidate))
                        return candidate;
                }
            }
            throw new InvalidOperationException("Failed to generate a unique game code.");
        }
    }
}
