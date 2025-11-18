
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class InMemoryGameSessionStore : IGameSessionStore
    {
        private readonly ConcurrentDictionary<int, GameSession> sessions =
            new ConcurrentDictionary<int, GameSession>();

        public GameSession CreateSession(
            int gameId,
            BoardDefinitionDto boardDefinition,
            IEnumerable<int> playerUserIds)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId), "GameId must be greater than zero.");
            }

            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            var players = (playerUserIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (players.Count == 0)
            {
                throw new InvalidOperationException("Cannot create a game session without players.");
            }

            var session = new GameSession
            {
                GameId = gameId,
                Board = boardDefinition,
                PlayerUserIds = players,
                CurrentTurnUserId = players[0],
                IsFinished = false
            };

            sessions.AddOrUpdate(gameId, session, (_, __) => session);

            return session;
        }

        public bool TryGetSession(int gameId, out GameSession session)
        {
            if (gameId <= 0)
            {
                session = null;
                return false;
            }

            return sessions.TryGetValue(gameId, out session);
        }

        public void UpdateSession(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            sessions.AddOrUpdate(session.GameId, session, (_, __) => session);
        }
    }
}
