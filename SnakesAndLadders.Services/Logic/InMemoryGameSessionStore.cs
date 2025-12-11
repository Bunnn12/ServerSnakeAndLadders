using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class InMemoryGameSessionStore : IGameSessionStore
    {
        private readonly ConcurrentDictionary<int, GameSession> _sessions =
            new ConcurrentDictionary<int, GameSession>();

        public GameSession CreateSession(
            int gameId,
            BoardDefinitionDto boardDefinition,
            IEnumerable<int> playerUserIds)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameId),
                    GameSessionStoreConstants.ERROR_GAME_ID_POSITIVE);
            }

            if (boardDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(boardDefinition),
                    GameSessionStoreConstants.ERROR_BOARD_REQUIRED);
            }

            var players = (playerUserIds ?? Enumerable.Empty<int>())
                .Where(id => id != GameSessionStoreConstants.INVALID_USER_ID)
                .Distinct()
                .ToList();

            if (players.Count == 0)
            {
                throw new InvalidOperationException(GameSessionStoreConstants.ERROR_NO_PLAYERS);
            }

            var session = new GameSession
            {
                GameId = gameId,
                Board = boardDefinition,
                PlayerUserIds = players,
                CurrentTurnUserId = players[0],
                IsFinished = false,
                WinnerUserId = GameSessionStoreConstants.INVALID_USER_ID,
                EndReason = null
            };

            _sessions.AddOrUpdate(gameId, session, (_, __) => session);

            return session;
        }

        public bool TryGetSession(int gameId, out GameSession session)
        {
            if (gameId <= 0)
            {
                session = null;
                return false;
            }

            return _sessions.TryGetValue(gameId, out session);
        }

        public void UpdateSession(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _sessions.AddOrUpdate(session.GameId, session, (_, __) => session);
        }
    }
}
