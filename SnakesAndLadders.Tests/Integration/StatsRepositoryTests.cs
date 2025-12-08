using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class StatsRepositoryTests : IntegrationTestBase
    {
        private const int INVALID_MAX_RESULTS_ZERO = 0;
        private const int INVALID_MAX_RESULTS_NEGATIVE = -1;

        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const int INVALID_RANKING_MAX_RESULTS_ZERO = 0;
        private const int INVALID_RANKING_MAX_RESULTS_NEGATIVE = -1;

        private const int TOTAL_USERS_FOR_CAP_TEST = 120;
        private const int REQUESTED_MAX_RESULTS_ABOVE_LIMIT = 150;
        private const int EXPECTED_MAX_ALLOWED_RESULTS = 100;

        private const int DEFAULT_RANKING_MAX_RESULTS = 10;

        private const int HIGH_COINS_VALUE = 100;
        private const int LOW_COINS_VALUE = 50;

        private const int MATCHES_PLAYED_FOR_STATS = 3;
        private const int MATCHES_WON_FOR_STATS = 2;

        private const byte STATUS_ACTIVE = 0x01;

        private const string BASE_USERNAME = "StatsUser";
        private const string BASE_FIRST_NAME = "Stats";
        private const string BASE_LAST_NAME = "User";

        private const byte WINNER_FLAG_VALUE = 0x01;
        private const byte LOSER_FLAG_VALUE = 0x00;

        private StatsRepository CreateRepository()
        {
            return new StatsRepository(CreateContext);
        }

        private int CreateTestUser(int coins)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);

                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = coins,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        private void RemoveUserById(int userId)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = db.Usuario.SingleOrDefault(u => u.IdUsuario == userId);

                if (user != null)
                {
                    db.Usuario.Remove(user);
                    db.SaveChanges();
                }
            }
        }

        private void RemoveMatchesForUser(int userId)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                List<UsuarioHasPartida> links = db.UsuarioHasPartida
                    .Where(link => link.UsuarioIdUsuario == userId)
                    .ToList();

                if (links.Count > 0)
                {
                    db.UsuarioHasPartida.RemoveRange(links);
                    db.SaveChanges();
                }
            }
        }

        private void CreateCompletedMatchForUser(
            int userId,
            byte winnerFlagValue,
            DateTime finishDateUtc)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Partida match = new Partida
                {
                    FechaTermino = finishDateUtc
                };

                db.Partida.Add(match);
                db.SaveChanges();

                UsuarioHasPartida link = new UsuarioHasPartida
                {
                    UsuarioIdUsuario = userId,
                    PartidaIdPartida = match.IdPartida,
                    Ganador = new[] { winnerFlagValue }
                };

                db.UsuarioHasPartida.Add(link);
                db.SaveChanges();
            }
        }

        [Theory]
        [InlineData(INVALID_MAX_RESULTS_ZERO)]
        [InlineData(INVALID_MAX_RESULTS_NEGATIVE)]
        public void TestGetTopPlayersByCoinsWhenMaxResultsLessOrEqualZeroThrowsArgumentOutOfRangeException(
            int invalidMaxResults)
        {
            StatsRepository repository = CreateRepository();

            Action action = () => repository.GetTopPlayersByCoins(invalidMaxResults);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetTopPlayersByCoinsWhenMaxResultsExceedsMaxAllowedCapsResultsCount()
        {
            for (int index = 0; index < TOTAL_USERS_FOR_CAP_TEST; index++)
            {
                int coins = HIGH_COINS_VALUE + index;
                CreateTestUser(coins);
            }

            StatsRepository repository = CreateRepository();

            IList<PlayerRankingItemDto> result =
                repository.GetTopPlayersByCoins(REQUESTED_MAX_RESULTS_ABOVE_LIMIT);

            Assert.Equal(EXPECTED_MAX_ALLOWED_RESULTS, result.Count);
        }

        [Fact]
        public void TestGetTopPlayersByCoinsWhenUsersExistReturnsOrderedByCoinsThenUsername()
        {
            string userNameHighA = $"{BASE_USERNAME}_HighA";
            string userNameHighB = $"{BASE_USERNAME}_HighB";
            string userNameLow = $"{BASE_USERNAME}_Low";

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userHighA = new Usuario
                {
                    NombreUsuario = userNameHighA,
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = HIGH_COINS_VALUE,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                Usuario userHighB = new Usuario
                {
                    NombreUsuario = userNameHighB,
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = HIGH_COINS_VALUE,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                Usuario userLow = new Usuario
                {
                    NombreUsuario = userNameLow,
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = LOW_COINS_VALUE,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(userHighA);
                db.Usuario.Add(userHighB);
                db.Usuario.Add(userLow);
                db.SaveChanges();
            }

            StatsRepository repository = CreateRepository();

            IList<PlayerRankingItemDto> rankingItems = repository.GetTopPlayersByCoins(3);

            bool isOk =
                rankingItems != null &&
                rankingItems.Count >= 3 &&
                rankingItems[0].Coins == HIGH_COINS_VALUE &&
                rankingItems[1].Coins == HIGH_COINS_VALUE &&
                string.Compare(
                    rankingItems[0].Username,
                    rankingItems[1].Username,
                    StringComparison.Ordinal) < 0 &&
                rankingItems[2].Coins == LOW_COINS_VALUE;

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetPlayerStatsByUserIdWhenUserIdLessOrEqualZeroThrowsArgumentOutOfRangeException(
            int invalidUserId)
        {
            StatsRepository repository = CreateRepository();

            Action action = () => repository.GetPlayerStatsByUserId(
                invalidUserId,
                DEFAULT_RANKING_MAX_RESULTS);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Theory]
        [InlineData(INVALID_RANKING_MAX_RESULTS_ZERO)]
        [InlineData(INVALID_RANKING_MAX_RESULTS_NEGATIVE)]
        public void TestGetPlayerStatsByUserIdWhenRankingMaxResultsLessOrEqualZeroThrowsArgumentOutOfRangeException(
            int invalidRankingMaxResults)
        {
            int userId = CreateTestUser(LOW_COINS_VALUE);

            StatsRepository repository = CreateRepository();

            Action action = () => repository.GetPlayerStatsByUserId(
                userId,
                invalidRankingMaxResults);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void TestGetPlayerStatsByUserIdWhenUserDoesNotExistReturnsDefaultStats()
        {
            int userId = CreateTestUser(LOW_COINS_VALUE);

            RemoveMatchesForUser(userId);
            RemoveUserById(userId);

            StatsRepository repository = CreateRepository();

            PlayerStatsDto stats = repository.GetPlayerStatsByUserId(
                userId,
                DEFAULT_RANKING_MAX_RESULTS);

            bool isOk =
                stats != null &&
                stats.UserId == userId &&
                string.IsNullOrEmpty(stats.Username) &&
                stats.Coins == 0 &&
                stats.MatchesPlayed == 0 &&
                stats.MatchesWon == 0 &&
                stats.WinPercentage == 0m &&
                stats.RankingPosition == 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetPlayerStatsByUserIdWhenUserHasNoMatchesReturnsZeroMatchesAndWinPercentage()
        {
            int userId = CreateTestUser(LOW_COINS_VALUE);

            RemoveMatchesForUser(userId);

            StatsRepository repository = CreateRepository();

            PlayerStatsDto stats = repository.GetPlayerStatsByUserId(
                userId,
                DEFAULT_RANKING_MAX_RESULTS);

            bool isOk =
                stats != null &&
                stats.UserId == userId &&
                stats.MatchesPlayed == 0 &&
                stats.MatchesWon == 0 &&
                stats.WinPercentage == 0m;

            Assert.True(isOk);
        }

    }
}
