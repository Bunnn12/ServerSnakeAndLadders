using System;
using System.Collections.Generic;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Wcf.Lobby
{
    public sealed class LobbyCleanupService : ILobbyCleanupService
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(LobbyCleanupService));

        private readonly ILobbyStore _lobbyStore;

        public LobbyCleanupService(ILobbyStore lobbyStore)
        {
            _lobbyStore = lobbyStore ?? throw new ArgumentNullException(nameof(lobbyStore));
        }

        public IReadOnlyCollection<LobbyInfo> CleanupExpiredLobbies()
        {
            DateTime nowUtc = DateTime.UtcNow;
            List<LobbyInfo> closedLobbies = new List<LobbyInfo>();

            foreach (LobbyInfo lobby in _lobbyStore.GetAll())
            {
                if (IsLobbyExpired(lobby, nowUtc) &&
                    lobby.Status != LobbyStatus.Closed)
                {
                    _logger.InfoFormat(
                        "CleanupExpiredLobbies: cerrando lobby expirado. " +
                        "PartidaId={0}, ExpiresAtUtc={1:u}",
                        lobby.PartidaId,
                        lobby.ExpiresAtUtc);

                    lobby.Status = LobbyStatus.Closed;
                    _lobbyStore.RemoveLobby(lobby.PartidaId);
                    closedLobbies.Add(lobby);
                }
            }

            return closedLobbies;
        }

        private static bool IsLobbyExpired(LobbyInfo lobby, DateTime nowUtc)
        {
            if (lobby == null)
            {
                return true;
            }

            return lobby.ExpiresAtUtc <= nowUtc;
        }
    }
}
