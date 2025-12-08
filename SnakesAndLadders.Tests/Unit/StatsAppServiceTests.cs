using System;
using System.Collections.Generic;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class StatsAppServiceTests
    {
        private const int VALID_USER_ID = 10;
        private const int OTHER_VALID_USER_ID = 20;

        private const int INVALID_ID_ZERO = 0;
        private const int INVALID_ID_NEGATIVE = -1;

        private const int DEFAULT_MAX_RESULTS = 50;
        private const int DEFAULT_STATS_RANKING_MAX_RESULTS = 50;

        private const int CUSTOM_MAX_RESULTS = 10;

        private readonly Mock<IStatsRepository> _statsRepositoryMock;
        private readonly StatsAppService _service;

        public StatsAppServiceTests()
        {
            _statsRepositoryMock = new Mock<IStatsRepository>(MockBehavior.Strict);
            _service = new StatsAppService(_statsRepositoryMock.Object);
        }

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenStatsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new StatsAppService(null));

            Assert.Equal("statsRepository", ex.ParamName);
        }

        #endregion

        #region GetTopPlayersByCoins

        [Fact]
        public void TestGetTopPlayersByCoinsUsesProvidedMaxWhenPositive()
        {
            var expectedList = new List<PlayerRankingItemDto>
            {
                new PlayerRankingItemDto
                {
                    UserId = VALID_USER_ID,
                    Username = "UserOne",
                    Coins = 500
                }
            };

            _statsRepositoryMock
                .Setup(r => r.GetTopPlayersByCoins(CUSTOM_MAX_RESULTS))
                .Returns(expectedList);

            IList<PlayerRankingItemDto> result =
                _service.GetTopPlayersByCoins(CUSTOM_MAX_RESULTS);

            Assert.Same(expectedList, result);

            _statsRepositoryMock.Verify(
                r => r.GetTopPlayersByCoins(CUSTOM_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void TestGetTopPlayersByCoinsUsesDefaultWhenMaxIsNonPositive(int maxResults)
        {
            var expectedList = new List<PlayerRankingItemDto>
            {
                new PlayerRankingItemDto
                {
                    UserId = OTHER_VALID_USER_ID,
                    Username = "UserTwo",
                    Coins = 1000
                }
            };

            _statsRepositoryMock
                .Setup(r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS))
                .Returns(expectedList);

            IList<PlayerRankingItemDto> result =
                _service.GetTopPlayersByCoins(maxResults);

            Assert.Same(expectedList, result);

            _statsRepositoryMock.Verify(
                r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetTopPlayersByCoinsReturnsNullWhenRepositoryReturnsNull()
        {
            _statsRepositoryMock
                .Setup(r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS))
                .Returns((IList<PlayerRankingItemDto>)null);

            IList<PlayerRankingItemDto> result =
                _service.GetTopPlayersByCoins(0);

            Assert.Null(result);

            _statsRepositoryMock.Verify(
                r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetTopPlayersByCoinsPropagatesRepositoryException()
        {
            _statsRepositoryMock
                .Setup(r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS))
                .Throws(new InvalidOperationException("Stats error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.GetTopPlayersByCoins(0));

            Assert.Equal("Stats error", ex.Message);

            _statsRepositoryMock.Verify(
                r => r.GetTopPlayersByCoins(DEFAULT_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GetPlayerStatsByUserId

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestGetPlayerStatsByUserIdThrowsWhenUserIdInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetPlayerStatsByUserId(userId));

            Assert.Equal("userId", ex.ParamName);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetPlayerStatsByUserIdDelegatesToRepositoryWithDefaultRankingLimit()
        {
            var expected = new PlayerStatsDto();

            _statsRepositoryMock
                .Setup(r => r.GetPlayerStatsByUserId(
                    VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS))
                .Returns(expected);

            PlayerStatsDto result = _service.GetPlayerStatsByUserId(VALID_USER_ID);

            Assert.Same(expected, result);

            _statsRepositoryMock.Verify(
                r => r.GetPlayerStatsByUserId(
                    VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetPlayerStatsByUserIdReturnsNullWhenRepositoryReturnsNull()
        {
            _statsRepositoryMock
                .Setup(r => r.GetPlayerStatsByUserId(
                    OTHER_VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS))
                .Returns((PlayerStatsDto)null);

            PlayerStatsDto result = _service.GetPlayerStatsByUserId(OTHER_VALID_USER_ID);

            Assert.Null(result);

            _statsRepositoryMock.Verify(
                r => r.GetPlayerStatsByUserId(
                    OTHER_VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetPlayerStatsByUserIdPropagatesRepositoryException()
        {
            _statsRepositoryMock
                .Setup(r => r.GetPlayerStatsByUserId(
                    VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS))
                .Throws(new InvalidOperationException("Stats detail error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.GetPlayerStatsByUserId(VALID_USER_ID));

            Assert.Equal("Stats detail error", ex.Message);

            _statsRepositoryMock.Verify(
                r => r.GetPlayerStatsByUserId(
                    VALID_USER_ID,
                    DEFAULT_STATS_RANKING_MAX_RESULTS),
                Times.Once);

            _statsRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion
    }
}
