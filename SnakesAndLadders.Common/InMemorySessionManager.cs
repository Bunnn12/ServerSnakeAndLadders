using System;
using System.Collections.Concurrent;
using log4net;

namespace ServerSnakesAndLadders.Common
{
    public static class InMemorySessionManager
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(InMemorySessionManager));

        private const int SessionTimeoutMinutes = 30;

        private static readonly ConcurrentDictionary<int, UserSessionInfo> _sessions =
            new ConcurrentDictionary<int, UserSessionInfo>();

        public static bool TryRegisterNewSession(int userId, string token, out string errorCode)
        {
            errorCode = string.Empty;

            DateTime nowUtc = DateTime.UtcNow;

            UserSessionInfo existingSession;
            if (_sessions.TryGetValue(userId, out existingSession))
            {
                if (IsSessionActive(existingSession, nowUtc))
                {
                    errorCode = "SESSION_ALREADY_ACTIVE";
                    _logger.WarnFormat(
                        "User {0} tried to login but already has an active session.",
                        userId);
                    return false;
                }

                // Sesión vieja/expirada → la quitamos y dejamos pasar
                UserSessionInfo removedSession;
                _sessions.TryRemove(userId, out removedSession);
            }

            var newSession = new UserSessionInfo
            {
                UserId = userId,
                Token = token,
                CreatedAtUtc = nowUtc,
                LastActivityUtc = nowUtc,
                IsActive = true
            };

            _sessions[userId] = newSession;

            _logger.InfoFormat("Session created in memory for user {0}.", userId);
            return true;
        }

        public static bool ValidateToken(int userId, string token)
        {
            DateTime nowUtc = DateTime.UtcNow;

            UserSessionInfo session;
            if (!_sessions.TryGetValue(userId, out session))
            {
                return false;
            }

            if (!session.IsActive)
            {
                return false;
            }

            if (!string.Equals(session.Token, token, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsSessionActive(session, nowUtc))
            {
                session.IsActive = false;
                _sessions[userId] = session;
                return false;
            }

            // Refrescamos último uso
            session.LastActivityUtc = nowUtc;
            _sessions[userId] = session;

            return true;
        }

        public static void Logout(int userId, string token)
        {
            UserSessionInfo session;
            if (_sessions.TryGetValue(userId, out session))
            {
                if (string.Equals(session.Token, token, StringComparison.Ordinal))
                {
                    session.IsActive = false;
                    _sessions[userId] = session;

                    UserSessionInfo removedSession;
                    _sessions.TryRemove(userId, out removedSession);

                    _logger.InfoFormat("Session removed from memory for user {0}.", userId);
                }
            }
        }

        private static bool IsSessionActive(UserSessionInfo session, DateTime nowUtc)
        {
            TimeSpan elapsed = nowUtc - session.LastActivityUtc;
            return session.IsActive &&
                   elapsed.TotalMinutes <= SessionTimeoutMinutes;
        }

        private struct UserSessionInfo
        {
            public int UserId { get; set; }
            public string Token { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime LastActivityUtc { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
