using ServerSnakesAndLadders;
using ServerSnakesAndLadders.Common;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Tests.integration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class GameResultsRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE = 0x01;
        private const int INITIAL_COINS = 0;

        private const int INVALID_GAME_ID_ZERO = 0;
        private const int INVALID_GAME_ID_NEGATIVE = -1;

        private const string BASE_USERNAME = "ResultsUser";
        private const string BASE_FIRST_NAME = "Results";
        private const string BASE_LAST_NAME = "User";

        private const string GAME_CODE_BASE = "GR0001";

        private const int COINS_WINNER_REWARD = 10;
        private const int COINS_OTHER_REWARD = 5;

        private GameResultsRepository CreateRepository()
        {
            return new GameResultsRepository(CreateContext);
        }

        private int CreateUser(string suffix, int initialCoins)
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
                    Monedas = initialCoins,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        private int CreateUser(string suffix)
        {
            return CreateUser(suffix, INITIAL_COINS);
        }

        private int InsertGame(string code, byte status)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida game = new Partida
                {
                    CodigoPartida = code,
                    Dificultad = LobbyRepositoryConstants.DEFAULT_DIFFICULTY,
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

        private void InsertUserGameLink(int gameId, int userId)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                UsuarioHasPartida link = new UsuarioHasPartida
                {
                    PartidaIdPartida = gameId,
                    UsuarioIdUsuario = userId,
                    esHost = false,
                    Ganador = null
                };

                db.UsuarioHasPartida.Add(link);
                db.SaveChanges();
            }
        }

        [Theory]
        [InlineData(INVALID_GAME_ID_ZERO)]
        [InlineData(INVALID_GAME_ID_NEGATIVE)]
        public void TestFinalizeGameWhenGameIdNotPositiveReturnsFailure(
            int invalidGameId)
        {
            GameResultsRepository repository = CreateRepository();

            OperationResult<bool> result = repository.FinalizeGame(
                invalidGameId,
                winnerUserId: 1,
                coinsByUserId: null);

            bool isOk =
                result != null &&
                !result.IsSuccess &&
                string.Equals(
                    result.ErrorMessage,
                    GameResultsConstants.ERROR_GAME_ID_POSITIVE,
                    StringComparison.Ordinal);

            Assert.True(isOk);
        }

        [Fact]
        public void TestFinalizeGameWhenGameDoesNotExistReturnsFailure()
        {
            GameResultsRepository repository = CreateRepository();

            int nonExistingGameId = 999999;
            int winnerUserId = 1;

            var coinsByUserId = new Dictionary<int, int>();

            OperationResult<bool> result = repository.FinalizeGame(
                nonExistingGameId,
                winnerUserId,
                coinsByUserId);

            bool isOk =
                result != null &&
                !result.IsSuccess &&
                string.Equals(
                    result.ErrorMessage,
                    GameResultsConstants.ERROR_GAME_NOT_FOUND,
                    StringComparison.Ordinal);

            Assert.True(isOk);
        }

        [Fact]
        public void TestFinalizeGameWhenContextFactoryThrowsReturnsFatalFailure()
        {
            GameResultsRepository repository = new GameResultsRepository(
                contextFactory: () =>
                {
                    throw new InvalidOperationException("Boom");
                });

            OperationResult<bool> result = repository.FinalizeGame(
                gameId: 1,
                winnerUserId: 1,
                coinsByUserId: null);

            bool isOk =
                result != null &&
                !result.IsSuccess &&
                string.Equals(
                    result.ErrorMessage,
                    GameResultsConstants.ERROR_FATAL_FINALIZING_GAME,
                    StringComparison.Ordinal);

            Assert.True(isOk);
        }

        [Fact]
        public void TestFinalizeGameWhenGameExistsUpdatesStatusAndCoins()
        {
            int winnerUserId = CreateUser("Winner", INITIAL_COINS);
            int otherUserId = CreateUser("Other", INITIAL_COINS);

            int gameId = InsertGame(
                GAME_CODE_BASE,
                GameResultsConstants.LOBBY_STATUS_CLOSED);

            InsertUserGameLink(gameId, winnerUserId);
            InsertUserGameLink(gameId, otherUserId);

            var coinsByUserId = new Dictionary<int, int>
            {
                { winnerUserId, COINS_WINNER_REWARD },
                { otherUserId, COINS_OTHER_REWARD }
            };

            GameResultsRepository repository = CreateRepository();

            OperationResult<bool> result = repository.FinalizeGame(
                gameId,
                winnerUserId,
                coinsByUserId);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida game = db.Partida.Single(p => p.IdPartida == gameId);
                Usuario winner = db.Usuario.Single(u => u.IdUsuario == winnerUserId);
                Usuario other = db.Usuario.Single(u => u.IdUsuario == otherUserId);

                bool isOk =
                    result != null &&
                    result.IsSuccess &&
                    game.EstadoPartida == GameResultsConstants.LOBBY_STATUS_CLOSED &&
                    game.FechaTermino.HasValue &&
                    winner.Monedas > INITIAL_COINS &&
                    other.Monedas > INITIAL_COINS;

                Assert.True(isOk);
            }
        }
    }
}
