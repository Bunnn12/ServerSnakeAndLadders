using System;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class LobbyRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE = 0x01;
        private const int INITIAL_COINS = 0;

        private const int INVALID_ID_ZERO = 0;
        private const int INVALID_ID_NEGATIVE = -1;

        private const byte CUSTOM_INITIAL_STATUS = 0x02;

        private const string BASE_USERNAME = "LobbyUser";
        private const string BASE_FIRST_NAME = "Lobby";
        private const string BASE_LAST_NAME = "User";

        private const string GAME_CODE_EXISTS = "EX0001";
        private const string GAME_CODE_TRIM = "EX0002";
        private const string GAME_CODE_STATUS_1 = "EX0003";
        private const string GAME_CODE_STATUS_2 = "EX0004";
        private const string GAME_CODE_ADD_USER_1 = "EX0005";
        private const string GAME_CODE_ADD_USER_2 = "EX0006";
        private const string GAME_CODE_HOST_TRUE = "EX0007";
        private const string GAME_CODE_HOST_FALSE = "EX0008";
        private const string GAME_CODE_HOST_NONE = "EX0009";
        private const string GAME_CODE_IN_LOBBY = "EX0010";
        private const string GAME_CODE_IN_LOBBY_NONE = "EX0011";
        private const string GAME_CODE_REMOVE_USER = "EX0012";
        private const string GAME_CODE_REMOVE_EXISTING = "EX0013";
        private const string GAME_CODE_REMOVE_NONE = "EX0014";

        private LobbyRepository CreateRepository()
        {
            return new LobbyRepository(CreateContext);
        }

        private int CreateUser(string suffix)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        private int CreateUser()
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return CreateUser(suffix);
        }

        private int InsertGame(
            string code,
            string difficulty,
            byte status)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida game = new Partida
                {
                    CodigoPartida = code,
                    Dificultad = difficulty,
                    EstadoPartida = status,
                    fechaCreacion = DateTime.UtcNow,
                    FechaInicio = null,
                    FechaTermino = null,
                    expiraEn = DateTime.UtcNow.AddMinutes(10)
                };

                db.Partida.Add(game);
                db.SaveChanges();

                return game.IdPartida;
            }
        }

        private void InsertUserGameLink(int gameId, int userId, bool isHost)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                UsuarioHasPartida link = new UsuarioHasPartida
                {
                    PartidaIdPartida = gameId,
                    UsuarioIdUsuario = userId,
                    esHost = isHost
                };

                db.UsuarioHasPartida.Add(link);
                db.SaveChanges();
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestCodeExistsWhenCodeIsNullOrWhiteSpaceReturnsFalse(
            string code)
        {
            LobbyRepository repository = CreateRepository();

            bool exists = repository.CodeExists(code);

            bool isOk = exists == false;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCodeExistsWhenGameDoesNotExistReturnsFalse()
        {
            LobbyRepository repository = CreateRepository();

            bool exists = repository.CodeExists("ZZZ999");

            bool isOk = exists == false;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCodeExistsWhenGameExistsReturnsTrue()
        {
            InsertGame(
                GAME_CODE_EXISTS,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            LobbyRepository repository = CreateRepository();

            bool exists = repository.CodeExists(GAME_CODE_EXISTS);

            bool isOk = exists;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCodeExistsTrimsCodeWhitespace()
        {
            InsertGame(
                GAME_CODE_TRIM,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            LobbyRepository repository = CreateRepository();

            bool exists = repository.CodeExists("   " + GAME_CODE_TRIM + "   ");

            bool isOk = exists;
            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateGameWhenRequestIsNullThrowsArgumentNullException()
        {
            LobbyRepository repository = CreateRepository();

            Action action = () => repository.CreateGame(null);

            bool throws =
                Assert.Throws<ArgumentNullException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestCreateGameWhenCodeIsNullOrWhiteSpaceThrowsArgumentException(
            string invalidCode)
        {
            int hostUserId = CreateUser();

            CreateLobbyRequestDto request = new CreateLobbyRequestDto
            {
                Code = invalidCode,
                HostUserId = hostUserId,
                Difficulty = LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };

            LobbyRepository repository = CreateRepository();

            Action action = () => repository.CreateGame(request);

            bool throws =
                Assert.Throws<ArgumentException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestCreateGameWhenHostUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidHostUserId)
        {
            CreateLobbyRequestDto request = new CreateLobbyRequestDto
            {
                Code = "CD0001",
                HostUserId = invalidHostUserId,
                Difficulty = LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
            };

            LobbyRepository repository = CreateRepository();

            Action action = () => repository.CreateGame(request);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestUpdateGameStatusWhenGameIdNotPositiveThrowsArgumentOutOfRange(
            int invalidGameId)
        {
            LobbyRepository repository = CreateRepository();

            Action action =
                () => repository.UpdateGameStatus(
                    invalidGameId,
                    LobbyStatus.Waiting);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestUpdateGameStatusWhenGameDoesNotExistDoesNotChangeOtherGames()
        {
            int gameId = InsertGame(
                GAME_CODE_STATUS_1,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                CUSTOM_INITIAL_STATUS);

            LobbyRepository repository = CreateRepository();

            int nonExistingId = gameId + 9999;

            repository.UpdateGameStatus(nonExistingId, LobbyStatus.Waiting);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida stored = db.Partida.Single(p => p.IdPartida == gameId);
                bool isOk = stored.EstadoPartida == CUSTOM_INITIAL_STATUS;
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestUpdateGameStatusWhenGameExistsUpdatesStatus()
        {
            int gameId = InsertGame(
                GAME_CODE_STATUS_2,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                CUSTOM_INITIAL_STATUS);

            LobbyRepository repository = CreateRepository();
            LobbyStatus mappedWaitingStatus =
                (LobbyStatus)LobbyRepositoryConstants.LOBBY_STATUS_WAITING;

            repository.UpdateGameStatus(gameId, mappedWaitingStatus);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida stored = db.Partida.Single(p => p.IdPartida == gameId);

                bool isOk = stored.EstadoPartida ==
                            LobbyRepositoryConstants.LOBBY_STATUS_WAITING;

                Assert.True(isOk);
            }
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestAddUserToGameWhenGameIdNotPositiveThrowsArgumentOutOfRange(
            int invalidGameId)
        {
            int userId = CreateUser();

            LobbyRepository repository = CreateRepository();

            Action action =
                () => repository.AddUserToGame(
                    invalidGameId,
                    userId,
                    isHost: false);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestAddUserToGameWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            int gameId = InsertGame(
                GAME_CODE_ADD_USER_1,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            LobbyRepository repository = CreateRepository();

            Action action =
                () => repository.AddUserToGame(
                    gameId,
                    invalidUserId,
                    isHost: false);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestAddUserToGameInsertsUserGameLinkWithHostFlag()
        {
            int gameId = InsertGame(
                GAME_CODE_ADD_USER_2,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser();

            LobbyRepository repository = CreateRepository();

            repository.AddUserToGame(gameId, userId, isHost: true);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                UsuarioHasPartida link = db.UsuarioHasPartida.SingleOrDefault(
                    l => l.PartidaIdPartida == gameId &&
                         l.UsuarioIdUsuario == userId);

                bool isOk =
                    link != null &&
                    link.esHost;

                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestIsUserHostReturnsTrueWhenUserIsHost()
        {
            int gameId = InsertGame(
                GAME_CODE_HOST_TRUE,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser("Host");

            InsertUserGameLink(gameId, userId, isHost: true);

            LobbyRepository repository = CreateRepository();

            bool isHost = repository.IsUserHost(gameId, userId);

            bool isOk = isHost;
            Assert.True(isOk);
        }

        [Fact]
        public void TestIsUserHostReturnsFalseWhenUserIsNotHost()
        {
            int gameId = InsertGame(
                GAME_CODE_HOST_FALSE,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int hostUserId = CreateUser("Host");
            int guestUserId = CreateUser("Guest");

            InsertUserGameLink(gameId, hostUserId, isHost: true);
            InsertUserGameLink(gameId, guestUserId, isHost: false);

            LobbyRepository repository = CreateRepository();

            bool isHost = repository.IsUserHost(gameId, guestUserId);

            bool isOk = isHost == false;
            Assert.True(isOk);
        }

        [Fact]
        public void TestIsUserHostReturnsFalseWhenUserNotInLobby()
        {
            int gameId = InsertGame(
                GAME_CODE_HOST_NONE,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser("User");

            LobbyRepository repository = CreateRepository();

            bool isHost = repository.IsUserHost(gameId, userId);

            bool isOk = isHost == false;
            Assert.True(isOk);
        }

        [Fact]
        public void TestIsUserInLobbyReturnsTrueWhenLinkExists()
        {
            int gameId = InsertGame(
                GAME_CODE_IN_LOBBY,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser("Player");

            InsertUserGameLink(gameId, userId, isHost: false);

            LobbyRepository repository = CreateRepository();

            bool isInLobby = repository.IsUserInLobby(gameId, userId);

            bool isOk = isInLobby;
            Assert.True(isOk);
        }

        [Fact]
        public void TestIsUserInLobbyReturnsFalseWhenLinkDoesNotExist()
        {
            int gameId = InsertGame(
                GAME_CODE_IN_LOBBY_NONE,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser("Player");

            LobbyRepository repository = CreateRepository();

            bool isInLobby = repository.IsUserInLobby(gameId, userId);

            bool isOk = isInLobby == false;
            Assert.True(isOk);
        }


        [Theory]
        [InlineData(INVALID_ID_ZERO, 1)]
        [InlineData(INVALID_ID_NEGATIVE, 1)]
        public void TestRemoveUserFromLobbyWhenLobbyIdNotPositiveThrowsArgumentOutOfRange(
            int invalidLobbyId,
            int dummyUserId)
        {
            LobbyRepository repository = CreateRepository();

            Action action =
                () => repository.RemoveUserFromLobby(
                    invalidLobbyId,
                    dummyUserId);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestRemoveUserFromLobbyWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            int gameId = InsertGame(
                GAME_CODE_REMOVE_USER,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            LobbyRepository repository = CreateRepository();

            Action action =
                () => repository.RemoveUserFromLobby(
                    gameId,
                    invalidUserId);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(action) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestRemoveUserFromLobbyWhenLinkExistsRemovesRow()
        {
            int gameId = InsertGame(
                GAME_CODE_REMOVE_EXISTING,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser();

            InsertUserGameLink(gameId, userId, isHost: false);

            LobbyRepository repository = CreateRepository();

            repository.RemoveUserFromLobby(gameId, userId);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool exists =
                    db.UsuarioHasPartida.Any(l =>
                        l.PartidaIdPartida == gameId &&
                        l.UsuarioIdUsuario == userId);

                bool isOk = exists == false;
                Assert.True(isOk);
            }
        }

        [Fact]
        public void TestRemoveUserFromLobbyWhenLinkDoesNotExistDoesNothing()
        {
            int gameId = InsertGame(
                GAME_CODE_REMOVE_NONE,
                LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
                LobbyRepositoryConstants.LOBBY_STATUS_WAITING);

            int userId = CreateUser();

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.UsuarioHasPartida.RemoveRange(db.UsuarioHasPartida);
                db.SaveChanges();
            }

            LobbyRepository repository = CreateRepository();

            repository.RemoveUserFromLobby(gameId, userId);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                bool hasLinks = db.UsuarioHasPartida.Any();
                bool isOk = hasLinks == false;
                Assert.True(isOk);
            }
        }
    }
}
