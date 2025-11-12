using System;
using log4net;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Wcf;

namespace SnakesAndLadders.Server.Helpers
{
    public sealed class PlayerSessionManager : IPlayerSessionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlayerSessionManager));

        private const string DEFAULT_KICK_REASON = "Automatic kick due to sanction.";

        private readonly LobbyService lobbyService;

        public PlayerSessionManager(LobbyService lobbyServiceValue)
        {
            lobbyService = lobbyServiceValue ?? throw new ArgumentNullException(nameof(lobbyServiceValue));
        }

        public void KickUserFromAllSessions(int userId, string reason)
        {
            if (userId <= 0)
            {
                return;
            }

            string safeReason = string.IsNullOrWhiteSpace(reason)
                ? DEFAULT_KICK_REASON
                : reason.Trim();

            try
            {
                lobbyService.KickUserFromAllLobbies(userId, safeReason);

                Logger.InfoFormat(
                    "KickUserFromAllSessions invoked for user {0}. Reason={1}",
                    userId,
                    safeReason);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    string.Format(
                        "Error while kicking user {0} from sessions.",
                        userId),
                    ex);
            }
        }
    }
}
