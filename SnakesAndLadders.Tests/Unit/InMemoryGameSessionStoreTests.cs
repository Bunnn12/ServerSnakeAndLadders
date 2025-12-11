using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Constants;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class InMemoryGameSessionStoreTests
    {
        [Fact]
        public void CreateSession_InvalidGameId_ThrowsArgumentOutOfRangeException()
        {
            var store = new InMemoryGameSessionStore();
            var board = new BoardDefinitionDto();

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => store.CreateSession(0, board, new[] { 1, 2 }));

            Assert.Equal("gameId", ex.ParamName);
        }

        [Fact]
        public void CreateSession_NullBoard_ThrowsArgumentNullException()
        {
            var store = new InMemoryGameSessionStore();

            var ex = Assert.Throws<ArgumentNullException>(
                () => store.CreateSession(1, null, new[] { 1 }));

            Assert.Equal("boardDefinition", ex.ParamName);
        }

        [Fact]
        public void CreateSession_NoPlayers_ThrowsInvalidOperationException()
        {
            var store = new InMemoryGameSessionStore();
            var board = new BoardDefinitionDto();

            var ex = Assert.Throws<InvalidOperationException>(
                () => store.CreateSession(1, board, new[] { GameSessionStoreConstants.INVALID_USER_ID }));

            Assert.Contains("Cannot create a game session without players", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateSession_ValidInput_CreatesSessionAndFiltersAndDedupsPlayers()
        {
            var store = new InMemoryGameSessionStore();
            var board = new BoardDefinitionDto();

            var players = new List<int> { 0, 42, 17, 42 };

            var session = store.CreateSession(123, board, players);

            Assert.NotNull(session);
            Assert.Equal(123, session.GameId);
            Assert.Same(board, session.Board);

            var expectedPlayers = new[] { 42, 17 };
            Assert.Equal(expectedPlayers.Length, session.PlayerUserIds.Count);
            Assert.True(expectedPlayers.SequenceEqual(session.PlayerUserIds));

            Assert.Equal(expectedPlayers[0], session.CurrentTurnUserId);
            Assert.False(session.IsFinished);
            Assert.Equal(GameSessionStoreConstants.INVALID_USER_ID, session.WinnerUserId);
        }

        [Fact]
        public void TryGetSession_InvalidGameId_ReturnsFalse()
        {
            var store = new InMemoryGameSessionStore();

            var got = store.TryGetSession(0, out var session);

            Assert.False(got);
            Assert.Null(session);
        }

        [Fact]
        public void TryGetSession_AfterCreate_ReturnsTrueAndSameSession()
        {
            var store = new InMemoryGameSessionStore();
            var board = new BoardDefinitionDto();
            var sessionCreated = store.CreateSession(500, board, new[] { 11, 22 });

            var got = store.TryGetSession(500, out var session);

            Assert.True(got);
            Assert.Same(sessionCreated, session);
            Assert.Equal(2, session.PlayerUserIds.Count);
        }

        [Fact]
        public void UpdateSession_Null_ThrowsArgumentNullException()
        {
            var store = new InMemoryGameSessionStore();

            Assert.Throws<ArgumentNullException>(() => store.UpdateSession(null));
        }

        [Fact]
        public void UpdateSession_ReplacesExistingSession()
        {
            var store = new InMemoryGameSessionStore();
            var board = new BoardDefinitionDto();
            var session = store.CreateSession(77, board, new[] { 1, 2 });

            session.IsFinished = true;
            session.WinnerUserId = 2;
            store.UpdateSession(session);

            var got = store.TryGetSession(77, out var updated);

            Assert.True(got);
            Assert.True(updated.IsFinished);
            Assert.Equal(2, updated.WinnerUserId);
        }
    }
}