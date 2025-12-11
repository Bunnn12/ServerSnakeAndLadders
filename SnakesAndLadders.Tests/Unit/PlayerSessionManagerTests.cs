using System;
using Moq;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Server.Helpers;
using SnakesAndLadders.Services.Wcf;
using SnakesAndLadders.Services.Wcf.Lobby;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class PlayerSessionManagerTests
    {
        private const int INVALID_USER_ID = 0;
        private const int VALID_USER_ID = 42;

        #region Helper

        private static PlayerSessionManager CreateManager()
        {
            var lobbyAppServiceMock = new Mock<ILobbyAppService>();
            var lobbyStoreMock = new Mock<ILobbyStore>();
            var lobbyNotificationMock = new Mock<ILobbyNotification>();
            var lobbyIdGeneratorMock = new Mock<ILobbyIdGenerator>();

            var lobbyService = new LobbyService(
                lobbyAppServiceMock.Object,
                lobbyStoreMock.Object,
                lobbyNotificationMock.Object,
                lobbyIdGeneratorMock.Object);

            return new PlayerSessionManager(lobbyService);
        }

        #endregion

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenLobbyServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new PlayerSessionManager(null));

            Assert.Equal("lobbyServiceValue", ex.ParamName);
        }

        #endregion

        #region KickUserFromAllSessions – invalid user

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void TestKickUserFromAllSessionsDoesNotThrowWhenUserIdIsZeroOrNegative(
            int userId)
        {
            var manager = CreateManager();

            var ex = Record.Exception(
                () => manager.KickUserFromAllSessions(userId, "any reason"));

            Assert.Null(ex);
        }

        #endregion

        #region KickUserFromAllSessions – valid user, various reasons

        [Fact]
        public void TestKickUserFromAllSessionsDoesNotThrowWithValidUserAndNullReason()
        {
            var manager = CreateManager();

            var ex = Record.Exception(
                () => manager.KickUserFromAllSessions(VALID_USER_ID, null));

            Assert.Null(ex);
        }

        [Fact]
        public void TestKickUserFromAllSessionsDoesNotThrowWithValidUserAndEmptyReason()
        {
            var manager = CreateManager();

            var ex = Record.Exception(
                () => manager.KickUserFromAllSessions(VALID_USER_ID, string.Empty));

            Assert.Null(ex);
        }

        [Fact]
        public void TestKickUserFromAllSessionsDoesNotThrowWithValidUserAndWhitespaceReason()
        {
            var manager = CreateManager();

            var ex = Record.Exception(
                () => manager.KickUserFromAllSessions(VALID_USER_ID, "   "));

            Assert.Null(ex);
        }

        [Fact]
        public void TestKickUserFromAllSessionsDoesNotThrowWithValidUserAndCustomReason()
        {
            var manager = CreateManager();

            var ex = Record.Exception(
                () => manager.KickUserFromAllSessions(VALID_USER_ID, "too toxic"));

            Assert.Null(ex);
        }

        #endregion
    }
}
