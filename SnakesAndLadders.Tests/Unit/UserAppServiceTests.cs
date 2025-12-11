using System;
using Moq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class UserAppServiceTests
    {
        private const int VALID_USER_ID = 123;

        private const string USERNAME = "UserOne";
        private const string ERROR_USERNAME_REQUIRED = "UserName is required.";
        private const string ERROR_REQUEST_REQUIRED = "Request is required.";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAccountStatusRepository> _accountStatusRepositoryMock;

        private readonly UserAppService _service;

        public UserAppServiceTests()
        {
            _userRepositoryMock =
                new Mock<IUserRepository>(MockBehavior.Strict);

            _accountStatusRepositoryMock =
                new Mock<IAccountStatusRepository>(MockBehavior.Strict);

            _service = new UserAppService(
                _userRepositoryMock.Object,
                _accountStatusRepositoryMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenUserRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new UserAppService(
                    null,
                    _accountStatusRepositoryMock.Object));

            bool isOk = ex.ParamName == "userRepository";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenAccountStatusRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new UserAppService(
                    _userRepositoryMock.Object,
                    null));

            bool isOk = ex.ParamName == "accountStatusRepository";

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestGetProfileByUsernameThrowsWhenUsernameIsInvalid(string username)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _service.GetProfileByUsername(username));

            bool isOk =
                ex.ParamName == "username" &&
                ex.Message.Contains(ERROR_USERNAME_REQUIRED);

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetProfileByUsernameReturnsRepositoryResult()
        {
            var expected = new AccountDto();

            _userRepositoryMock
                .Setup(repo => repo.GetByUsername(USERNAME))
                .Returns(expected);

            AccountDto result = _service.GetProfileByUsername(USERNAME);

            _userRepositoryMock.Verify(
                repo => repo.GetByUsername(USERNAME),
                Times.Once);

            bool isOk =
                result != null &&
                ReferenceEquals(result, expected);

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestGetProfilePhotoThrowsWhenUserIdIsInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetProfilePhoto(userId));

            bool isOk =
                ex.ParamName == "userId" &&
                ex.Message.Contains(ERROR_USER_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetProfilePhotoReturnsRepositoryResult()
        {
            var expected = new ProfilePhotoDto();

            _userRepositoryMock
                .Setup(repo => repo.GetPhotoByUserId(VALID_USER_ID))
                .Returns(expected);

            ProfilePhotoDto result = _service.GetProfilePhoto(VALID_USER_ID);

            _userRepositoryMock.Verify(
                repo => repo.GetPhotoByUserId(VALID_USER_ID),
                Times.Once);

            bool isOk =
                result != null &&
                ReferenceEquals(result, expected);

            Assert.True(isOk);
        }


        [Fact]
        public void TestUpdateProfileThrowsWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _service.UpdateProfile(null));

            bool isOk =
                ex.ParamName == "request" &&
                ex.Message.Contains(ERROR_REQUEST_REQUIRED);

            Assert.True(isOk);
        }

        [Fact]
        public void TestUpdateProfileReturnsRepositoryResult()
        {
            var request = new UpdateProfileRequestDto();

            var expected = new AccountDto();

            _userRepositoryMock
                .Setup(repo => repo.UpdateProfile(request))
                .Returns(expected);

            AccountDto result = _service.UpdateProfile(request);

            _userRepositoryMock.Verify(
                repo => repo.UpdateProfile(request),
                Times.Once);

            bool isOk =
                result != null &&
                ReferenceEquals(result, expected);

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void TestDeactivateAccountThrowsWhenUserIdIsInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.DeactivateAccount(userId));

            bool isOk =
                ex.ParamName == "userId" &&
                ex.Message.Contains(ERROR_USER_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Fact]
        public void TestDeactivateAccountCallsRepositoryOnce()
        {
            _accountStatusRepositoryMock
                .Setup(repo => repo.DeactivateUserAndAccount(VALID_USER_ID));

            _service.DeactivateAccount(VALID_USER_ID);

            _accountStatusRepositoryMock.Verify(
                repo => repo.DeactivateUserAndAccount(VALID_USER_ID),
                Times.Once);

            bool isOk = true;

            Assert.True(isOk);
        }



        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void TestGetAvatarOptionsThrowsWhenUserIdIsInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetAvatarOptions(userId));

            bool isOk =
                ex.ParamName == "userId" &&
                ex.Message.Contains(ERROR_USER_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetAvatarOptionsReturnsRepositoryResult()
        {
            var expected = new AvatarProfileOptionsDto();

            _userRepositoryMock
                .Setup(repo => repo.GetAvatarOptions(VALID_USER_ID))
                .Returns(expected);

            AvatarProfileOptionsDto result =
                _service.GetAvatarOptions(VALID_USER_ID);

            _userRepositoryMock.Verify(
                repo => repo.GetAvatarOptions(VALID_USER_ID),
                Times.Once);

            bool isOk =
                result != null &&
                ReferenceEquals(result, expected);

            Assert.True(isOk);
        }


        [Fact]
        public void TestSelectAvatarForProfileThrowsWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _service.SelectAvatarForProfile(null));

            bool isOk =
                ex.ParamName == "request" &&
                ex.Message.Contains(ERROR_REQUEST_REQUIRED);

            Assert.True(isOk);
        }

        [Fact]
        public void TestSelectAvatarForProfileReturnsRepositoryResult()
        {
            var request = new AvatarSelectionRequestDto();

            var expected = new AccountDto();

            _userRepositoryMock
                .Setup(repo => repo.SelectAvatarForProfile(request))
                .Returns(expected);

            AccountDto result = _service.SelectAvatarForProfile(request);

            _userRepositoryMock.Verify(
                repo => repo.SelectAvatarForProfile(request),
                Times.Once);

            bool isOk =
                result != null &&
                ReferenceEquals(result, expected);

            Assert.True(isOk);
        }


    }
}
