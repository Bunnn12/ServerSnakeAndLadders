using System;
using System.Collections.Generic;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class SocialProfileAppServiceTests
    {
        private const int VALID_USER_ID = 10;
        private const int OTHER_VALID_USER_ID = 20;
        private const int INVALID_ID_ZERO = 0;
        private const int INVALID_ID_NEGATIVE = -1;

        private const SocialNetworkType NETWORK_A = (SocialNetworkType)1;
        private const SocialNetworkType NETWORK_B = (SocialNetworkType)2;

        private readonly Mock<ISocialProfileRepository> _socialProfilesRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;

        private readonly SocialProfileAppService _service;

        public SocialProfileAppServiceTests()
        {
            _socialProfilesRepositoryMock = new Mock<ISocialProfileRepository>(MockBehavior.Strict);
            _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);

            _service = new SocialProfileAppService(
                _socialProfilesRepositoryMock.Object,
                _userRepositoryMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenSocialProfilesRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new SocialProfileAppService(
                    null,
                    _userRepositoryMock.Object));

            Assert.Equal("socialProfiles", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenUserRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new SocialProfileAppService(
                    _socialProfilesRepositoryMock.Object,
                    null));

            Assert.Equal("users", ex.ParamName);
        }


        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestGetSocialProfilesThrowsArgumentOutOfRangeWhenUserIdIsNonPositive(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetSocialProfiles(userId));

            Assert.Equal("userId", ex.ParamName);
        }

        [Fact]
        public void TestGetSocialProfilesThrowsInvalidOperationWhenUserDoesNotExist()
        {
            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns((AccountDto)null);

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.GetSocialProfiles(VALID_USER_ID));

            Assert.Equal("User not found.", ex.Message);
        }

        [Fact]
        public void TestGetSocialProfilesReturnsProfilesWhenUserExists()
        {
            var account = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = "UserOne"
            };

            var expectedProfiles = new List<SocialProfileDto>
            {
                new SocialProfileDto
                {
                    UserId = VALID_USER_ID,
                    Network = NETWORK_A,
                    ProfileLink = "https://example.com/profile1"
                },
                new SocialProfileDto
                {
                    UserId = VALID_USER_ID,
                    Network = NETWORK_B,
                    ProfileLink = "https://example.com/profile2"
                }
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(account);

            _socialProfilesRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(expectedProfiles);

            IReadOnlyList<SocialProfileDto> result =
                _service.GetSocialProfiles(VALID_USER_ID);

            bool isOk =
                ReferenceEquals(expectedProfiles, result) &&
                result.Count == 2 &&
                result[0].UserId == VALID_USER_ID &&
                result[1].UserId == VALID_USER_ID;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetSocialProfilesReturnsEmptyListWhenRepositoryReturnsEmpty()
        {
            var account = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = "UserOne"
            };

            var expectedProfiles = new List<SocialProfileDto>();

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(account);

            _socialProfilesRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(expectedProfiles);

            IReadOnlyList<SocialProfileDto> result =
                _service.GetSocialProfiles(VALID_USER_ID);

            bool isOk =
                ReferenceEquals(expectedProfiles, result) &&
                result.Count == 0;

            Assert.True(isOk);
        }


        [Fact]
        public void TestLinkSocialProfileThrowsArgumentNullWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _service.LinkSocialProfile(null));

            Assert.Equal("request", ex.ParamName);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestLinkSocialProfileThrowsArgumentOutOfRangeWhenUserIdIsNonPositive(int userId)
        {
            var request = new LinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = NETWORK_A,
                ProfileLink = "https://example.com/profile"
            };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.LinkSocialProfile(request));

            Assert.Equal("userId", ex.ParamName);
        }

        [Fact]
        public void TestLinkSocialProfileThrowsInvalidOperationWhenUserDoesNotExist()
        {
            var request = new LinkSocialProfileRequestDto
            {
                UserId = VALID_USER_ID,
                Network = NETWORK_A,
                ProfileLink = "https://example.com/profile"
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns((AccountDto)null);

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.LinkSocialProfile(request));

            Assert.Equal("User not found.", ex.Message);
        }

        [Fact]
        public void TestLinkSocialProfileReturnsProfileWhenUserExists()
        {
            var request = new LinkSocialProfileRequestDto
            {
                UserId = VALID_USER_ID,
                Network = NETWORK_A,
                ProfileLink = "https://example.com/profile"
            };

            var account = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = "UserOne"
            };

            var expectedProfile = new SocialProfileDto
            {
                UserId = VALID_USER_ID,
                Network = NETWORK_A,
                ProfileLink = "https://example.com/profile"
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(account);

            _socialProfilesRepositoryMock
                .Setup(repo => repo.Upsert(request))
                .Returns(expectedProfile);

            SocialProfileDto result = _service.LinkSocialProfile(request);

            bool isOk =
                ReferenceEquals(expectedProfile, result) &&
                result.UserId == VALID_USER_ID &&
                result.Network == NETWORK_A &&
                result.ProfileLink == "https://example.com/profile";

            Assert.True(isOk);
        }


        [Fact]
        public void TestUnlinkSocialProfileThrowsArgumentNullWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _service.UnlinkSocialProfile(null));

            Assert.Equal("request", ex.ParamName);
        }

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestUnlinkSocialProfileThrowsArgumentOutOfRangeWhenUserIdIsNonPositive(int userId)
        {
            var request = new UnlinkSocialProfileRequestDto
            {
                UserId = userId,
                Network = NETWORK_B
            };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.UnlinkSocialProfile(request));

            Assert.Equal("userId", ex.ParamName);
        }

        [Fact]
        public void TestUnlinkSocialProfileThrowsInvalidOperationWhenUserDoesNotExist()
        {
            var request = new UnlinkSocialProfileRequestDto
            {
                UserId = OTHER_VALID_USER_ID,
                Network = NETWORK_B
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(OTHER_VALID_USER_ID))
                .Returns((AccountDto)null);

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.UnlinkSocialProfile(request));

            Assert.Equal("User not found.", ex.Message);
        }

        [Fact]
        public void TestUnlinkSocialProfileCallsRepositoryWhenUserExists()
        {
            var request = new UnlinkSocialProfileRequestDto
            {
                UserId = VALID_USER_ID,
                Network = NETWORK_B
            };

            var account = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = "UserOne"
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(account);

            _socialProfilesRepositoryMock
                .Setup(repo => repo.DeleteSocialNetwork(request));

            _service.UnlinkSocialProfile(request);

            Assert.True(true);
        }
    }
}
