using System;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class LobbyAppServiceTests
    {
        private const int VALID_GAME_ID = 10;
        private const int VALID_LOBBY_ID = 10;
        private const int VALID_USER_ID = 5;
        private const int OTHER_USER_ID = 7;

        private readonly Mock<ILobbyRepository> _lobbyRepositoryMock;
        private readonly Mock<IAppLogger> _appLoggerMock;

        private readonly LobbyAppService _service;

        public LobbyAppServiceTests()
        {
            _lobbyRepositoryMock =
                new Mock<ILobbyRepository>(MockBehavior.Strict);

            _appLoggerMock =
                new Mock<IAppLogger>(MockBehavior.Strict);

            _service = new LobbyAppService(
                _lobbyRepositoryMock.Object,
                _appLoggerMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenLobbyRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LobbyAppService(
                    null,
                    _appLoggerMock.Object));

            bool isOk = ex.ParamName == "lobbyRepository";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenAppLoggerIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LobbyAppService(
                    _lobbyRepositoryMock.Object,
                    null));

            bool isOk = ex.ParamName == "appLogger";

            Assert.True(isOk);
        }


        [Fact]
        public void TestCreateGameThrowsArgumentNullWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _service.CreateGame(null));

            bool isOk = ex.ParamName == "request";

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestCreateGameThrowsArgumentExceptionWhenHostUserIdIsInvalid(
            int invalidHostUserId)
        {
            var request = new CreateGameRequest
            {
                HostUserId = invalidHostUserId,
                MaxPlayers = 2,
                Dificultad = "Normal",
                TtlMinutes = 10
            };

            var ex = Assert.Throws<ArgumentException>(
                () => _service.CreateGame(request));

            bool isOk = ex.ParamName == "HostUserId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateGameThrowsArgumentExceptionWhenMaxPlayersIsOutOfRange()
        {
            var request = new CreateGameRequest
            {
                HostUserId = VALID_USER_ID,
                MaxPlayers = 0, // fuera de rango
                Dificultad = "Normal",
                TtlMinutes = 10
            };

            var ex = Assert.Throws<ArgumentException>(
                () => _service.CreateGame(request));

            bool isOk = ex.ParamName == "MaxPlayers";

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateGameReturnsResponseWhenRepositoryCreatesGame()
        {
            var request = new CreateGameRequest
            {
                HostUserId = VALID_USER_ID,
                MaxPlayers = 4,
                Dificultad = "Normal",
                TtlMinutes = 0 
            };

            _lobbyRepositoryMock
                .Setup(repo => repo.CodeExists(It.IsAny<string>()))
                .Returns(false);

            CreateLobbyRequestDto capturedLobbyRequest = null;

            var createdInfo = new CreatedGameInfo
            {
                PartidaId = 123,
                Code = "123456",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };

            _lobbyRepositoryMock
                .Setup(repo => repo.CreateGame(It.IsAny<CreateLobbyRequestDto>()))
                .Callback<CreateLobbyRequestDto>(dto =>
                {
                    capturedLobbyRequest = dto;
                })
                .Returns(createdInfo);

            _appLoggerMock
                .Setup(logger => logger.Info(It.IsAny<string>()));

            CreateGameResponse response = _service.CreateGame(request);

            bool isOk =
                response != null &&
                response.PartidaId == createdInfo.PartidaId &&
                response.CodigoPartida == createdInfo.Code &&
                response.ExpiresAtUtc == createdInfo.ExpiresAtUtc &&
                capturedLobbyRequest != null &&
                capturedLobbyRequest.HostUserId == request.HostUserId &&
                capturedLobbyRequest.MaxPlayers == request.MaxPlayers &&
                capturedLobbyRequest.Difficulty == request.Dificultad &&
                !string.IsNullOrWhiteSpace(capturedLobbyRequest.Code);

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateGameThrowsInvalidOperationWhenRepositoryThrowsConflict()
        {
            var request = new CreateGameRequest
            {
                HostUserId = VALID_USER_ID,
                MaxPlayers = 4,
                Dificultad = "Normal",
                TtlMinutes = 10
            };

            _lobbyRepositoryMock
                .Setup(repo => repo.CodeExists(It.IsAny<string>()))
                .Returns(false);

            var conflictException =
                new InvalidOperationException("Simulated conflict");

            _lobbyRepositoryMock
                .Setup(repo => repo.CreateGame(It.IsAny<CreateLobbyRequestDto>()))
                .Throws(conflictException);

            _appLoggerMock
                .Setup(logger => logger.Error(
                    It.IsAny<string>(),
                    conflictException));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.CreateGame(request));

            bool isOk = ReferenceEquals(ex.InnerException, conflictException);

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateGameThrowsInvalidOperationWhenCannotGenerateUniqueCode()
        {
            var request = new CreateGameRequest
            {
                HostUserId = VALID_USER_ID,
                MaxPlayers = 4,
                Dificultad = "Normal",
                TtlMinutes = 10
            };

            _lobbyRepositoryMock
                .Setup(repo => repo.CodeExists(It.IsAny<string>()))
                .Returns(true);


            _appLoggerMock
                .Setup(logger => logger.Info(It.IsAny<string>()));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.CreateGame(request));

            bool isOk = ex != null;

            Assert.True(isOk);
        }


        [Fact]
        public void TestRegisterHostInGameThrowsWhenGameIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.RegisterHostInGame(0, VALID_USER_ID));

            bool isOk = ex.ParamName == "gameId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterHostInGameThrowsWhenUserIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.RegisterHostInGame(VALID_GAME_ID, 0));

            bool isOk = ex.ParamName == "userId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterHostInGameCallsRepositoryAndLogs()
        {
            _lobbyRepositoryMock
                .Setup(repo => repo.AddUserToGame(
                    VALID_GAME_ID,
                    VALID_USER_ID,
                    true));

            _appLoggerMock
                .Setup(logger => logger.Info(It.IsAny<string>()));

            _service.RegisterHostInGame(VALID_GAME_ID, VALID_USER_ID);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterPlayerInGameThrowsWhenGameIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.RegisterPlayerInGame(0, VALID_USER_ID));

            bool isOk = ex.ParamName == "gameId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterPlayerInGameThrowsWhenUserIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.RegisterPlayerInGame(VALID_GAME_ID, 0));

            bool isOk = ex.ParamName == "userId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterPlayerInGameCallsRepositoryAndLogs()
        {
            _lobbyRepositoryMock
                .Setup(repo => repo.AddUserToGame(
                    VALID_GAME_ID,
                    VALID_USER_ID,
                    false));

            _appLoggerMock
                .Setup(logger => logger.Info(It.IsAny<string>()));

            _service.RegisterPlayerInGame(VALID_GAME_ID, VALID_USER_ID);

            bool isOk = true;

            Assert.True(isOk);
        }

 

        [Fact]
        public void TestKickPlayerFromLobbyThrowsWhenLobbyIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.KickPlayerFromLobby(0, VALID_USER_ID, OTHER_USER_ID));

            bool isOk = ex.ParamName == "lobbyId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyThrowsWhenHostUserIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.KickPlayerFromLobby(VALID_LOBBY_ID, 0, OTHER_USER_ID));

            bool isOk = ex.ParamName == "hostUserId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyThrowsWhenTargetUserIdIsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.KickPlayerFromLobby(VALID_LOBBY_ID, VALID_USER_ID, 0));

            bool isOk = ex.ParamName == "targetUserId";

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyThrowsWhenHostTriesToKickSelf()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.KickPlayerFromLobby(
                    VALID_LOBBY_ID,
                    VALID_USER_ID,
                    VALID_USER_ID));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyThrowsWhenHostIsNotValid()
        {
            _lobbyRepositoryMock
                .Setup(repo => repo.IsUserHost(
                    VALID_LOBBY_ID,
                    VALID_USER_ID))
                .Returns(false);

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.KickPlayerFromLobby(
                    VALID_LOBBY_ID,
                    VALID_USER_ID,
                    OTHER_USER_ID));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyDoesNothingWhenTargetNotInLobby()
        {
            _lobbyRepositoryMock
                .Setup(repo => repo.IsUserHost(
                    VALID_LOBBY_ID,
                    VALID_USER_ID))
                .Returns(true);

            _lobbyRepositoryMock
                .Setup(repo => repo.IsUserInLobby(
                    VALID_LOBBY_ID,
                    OTHER_USER_ID))
                .Returns(false);


            _service.KickPlayerFromLobby(
                VALID_LOBBY_ID,
                VALID_USER_ID,
                OTHER_USER_ID);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Fact]
        public void TestKickPlayerFromLobbyRemovesUserWhenHostValidAndTargetInLobby()
        {
            _lobbyRepositoryMock
                .Setup(repo => repo.IsUserHost(
                    VALID_LOBBY_ID,
                    VALID_USER_ID))
                .Returns(true);

            _lobbyRepositoryMock
                .Setup(repo => repo.IsUserInLobby(
                    VALID_LOBBY_ID,
                    OTHER_USER_ID))
                .Returns(true);

            _lobbyRepositoryMock
                .Setup(repo => repo.RemoveUserFromLobby(
                    VALID_LOBBY_ID,
                    OTHER_USER_ID));

            _service.KickPlayerFromLobby(
                VALID_LOBBY_ID,
                VALID_USER_ID,
                OTHER_USER_ID);

            bool isOk = true;

            Assert.True(isOk);
        }

    }
}
