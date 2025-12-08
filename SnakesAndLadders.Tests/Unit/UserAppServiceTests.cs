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
        private const int VALID_USER_ID = 10;
        private const int OTHER_VALID_USER_ID = 20;
        private const int INVALID_ID_ZERO = 0;
        private const int INVALID_ID_NEGATIVE = -1;

        private const string VALID_USERNAME = "UserOne";
        private const string OTHER_VALID_USERNAME = "UserTwo";

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAccountStatusRepository> _accountStatusRepositoryMock;

        private readonly UserAppService _service;

        public UserAppServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
            _accountStatusRepositoryMock = new Mock<IAccountStatusRepository>(MockBehavior.Strict);

            _service = new UserAppService(
                _userRepositoryMock.Object,
                _accountStatusRepositoryMock.Object);
        }

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenUserRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new UserAppService(null, _accountStatusRepositoryMock.Object));

            Assert.Equal("users", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenAccountStatusRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new UserAppService(_userRepositoryMock.Object, null));

            Assert.Equal("accountStatusRepository", ex.ParamName);
        }

        #endregion

        #region GetProfileByUsername

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestGetProfileByUsernameThrowsWhenInvalid(string username)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => _service.GetProfileByUsername(username));

            Assert.Equal("username", ex.ParamName);

            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetProfileByUsernameReturnsProfile()
        {
            var expected = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = VALID_USERNAME
            };

            _userRepositoryMock
                .Setup(r => r.GetByUsername(VALID_USERNAME))
                .Returns(expected);

            AccountDto result = _service.GetProfileByUsername(VALID_USERNAME);

            Assert.Same(expected, result);

            _userRepositoryMock.Verify(r => r.GetByUsername(VALID_USERNAME), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetProfileByUsernameReturnsNullWhenRepositoryReturnsNull()
        {
            _userRepositoryMock
                .Setup(r => r.GetByUsername(OTHER_VALID_USERNAME))
                .Returns((AccountDto)null);

            AccountDto result = _service.GetProfileByUsername(OTHER_VALID_USERNAME);

            Assert.Null(result);

            _userRepositoryMock.Verify(r => r.GetByUsername(OTHER_VALID_USERNAME), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GetProfilePhoto

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestGetProfilePhotoThrowsWhenIdInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetProfilePhoto(userId));

            Assert.Equal("userId", ex.ParamName);

            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetProfilePhotoReturnsData()
        {
            var expected = new ProfilePhotoDto
            {
                UserId = VALID_USER_ID,
                ProfilePhotoId = "PHOTO123"
            };

            _userRepositoryMock
                .Setup(r => r.GetPhotoByUserId(VALID_USER_ID))
                .Returns(expected);

            ProfilePhotoDto result = _service.GetProfilePhoto(VALID_USER_ID);

            Assert.Same(expected, result);

            _userRepositoryMock.Verify(r => r.GetPhotoByUserId(VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetProfilePhotoReturnsNullWhenRepositoryReturnsNull()
        {
            _userRepositoryMock
                .Setup(r => r.GetPhotoByUserId(OTHER_VALID_USER_ID))
                .Returns((ProfilePhotoDto)null);

            ProfilePhotoDto result = _service.GetProfilePhoto(OTHER_VALID_USER_ID);

            Assert.Null(result);

            _userRepositoryMock.Verify(r => r.GetPhotoByUserId(OTHER_VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetProfilePhotoPropagatesRepositoryException()
        {
            _userRepositoryMock
                .Setup(r => r.GetPhotoByUserId(VALID_USER_ID))
                .Throws(new InvalidOperationException("Test error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.GetProfilePhoto(VALID_USER_ID));

            Assert.Equal("Test error", ex.Message);

            _userRepositoryMock.Verify(r => r.GetPhotoByUserId(VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region UpdateProfile

        [Fact]
        public void TestUpdateProfileDelegatesToRepository()
        {
            var request = new UpdateProfileRequestDto
            {
                UserId = VALID_USER_ID,
                FirstName = "Juan",
                LastName = "Perez",
                ProfileDescription = "Hola",
                ProfilePhotoId = "A0002"
            };

            var expected = new AccountDto
            {
                UserId = VALID_USER_ID,
                ProfileDescription = "Hola",
                ProfilePhotoId = "A0002"
            };

            _userRepositoryMock
                .Setup(r => r.UpdateProfile(request))
                .Returns(expected);

            AccountDto result = _service.UpdateProfile(request);

            Assert.Same(expected, result);

            _userRepositoryMock.Verify(r => r.UpdateProfile(request), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestUpdateProfileAllowsNullRequestAndDelegates()
        {
            _userRepositoryMock
                .Setup(r => r.UpdateProfile(null))
                .Returns((AccountDto)null);

            AccountDto result = _service.UpdateProfile(null);

            Assert.Null(result);

            _userRepositoryMock.Verify(r => r.UpdateProfile(null), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestUpdateProfilePropagatesRepositoryException()
        {
            var request = new UpdateProfileRequestDto
            {
                UserId = VALID_USER_ID
            };

            _userRepositoryMock
                .Setup(r => r.UpdateProfile(request))
                .Throws(new InvalidOperationException("Update error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.UpdateProfile(request));

            Assert.Equal("Update error", ex.Message);

            _userRepositoryMock.Verify(r => r.UpdateProfile(request), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region DeactivateAccount

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestDeactivateAccountThrowsWhenIdInvalid(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.DeactivateAccount(userId));

            Assert.Equal("userId", ex.ParamName);

            _accountStatusRepositoryMock.VerifyNoOtherCalls();
            _userRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestDeactivateAccountCallsRepository()
        {
            _accountStatusRepositoryMock
                .Setup(r => r.SetUserAndAccountActiveState(VALID_USER_ID, false));

            _service.DeactivateAccount(VALID_USER_ID);

            _accountStatusRepositoryMock.Verify(
                r => r.SetUserAndAccountActiveState(VALID_USER_ID, false),
                Times.Once);

            _accountStatusRepositoryMock.VerifyNoOtherCalls();
            _userRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestDeactivateAccountPropagatesRepositoryException()
        {
            _accountStatusRepositoryMock
                .Setup(r => r.SetUserAndAccountActiveState(VALID_USER_ID, false))
                .Throws(new InvalidOperationException("Deactivate error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.DeactivateAccount(VALID_USER_ID));

            Assert.Equal("Deactivate error", ex.Message);

            _accountStatusRepositoryMock.Verify(
                r => r.SetUserAndAccountActiveState(VALID_USER_ID, false),
                Times.Once);

            _accountStatusRepositoryMock.VerifyNoOtherCalls();
            _userRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GetAvatarOptions

        [Theory]
        [InlineData(INVALID_ID_ZERO)]
        [InlineData(INVALID_ID_NEGATIVE)]
        public void TestGetAvatarOptionsThrowsInvalidId(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.GetAvatarOptions(userId));

            Assert.Equal("userId", ex.ParamName);

            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetAvatarOptionsReturnsData()
        {
            var expected = new AvatarProfileOptionsDto
            {
                UserId = VALID_USER_ID,
                Avatars = new[]
                {
                    new AvatarProfileOptionDto
                    {
                        AvatarCode = "A0001",
                        DisplayName = "Default",
                        IsUnlocked = true,
                        IsCurrent = true
                    }
                }
            };

            _userRepositoryMock
                .Setup(r => r.GetAvatarOptions(VALID_USER_ID))
                .Returns(expected);

            AvatarProfileOptionsDto result = _service.GetAvatarOptions(VALID_USER_ID);

            Assert.Same(expected, result);
            Assert.Equal(VALID_USER_ID, result.UserId);
            Assert.NotNull(result.Avatars);
            Assert.Single(result.Avatars);
            Assert.Equal("A0001", result.Avatars[0].AvatarCode);

            _userRepositoryMock.Verify(r => r.GetAvatarOptions(VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetAvatarOptionsReturnsNullWhenRepositoryReturnsNull()
        {
            _userRepositoryMock
                .Setup(r => r.GetAvatarOptions(OTHER_VALID_USER_ID))
                .Returns((AvatarProfileOptionsDto)null);

            AvatarProfileOptionsDto result = _service.GetAvatarOptions(OTHER_VALID_USER_ID);

            Assert.Null(result);

            _userRepositoryMock.Verify(r => r.GetAvatarOptions(OTHER_VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestGetAvatarOptionsPropagatesRepositoryException()
        {
            _userRepositoryMock
                .Setup(r => r.GetAvatarOptions(VALID_USER_ID))
                .Throws(new InvalidOperationException("Avatar error"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => _service.GetAvatarOptions(VALID_USER_ID));

            Assert.Equal("Avatar error", ex.Message);

            _userRepositoryMock.Verify(r => r.GetAvatarOptions(VALID_USER_ID), Times.Once);
            _userRepositoryMock.VerifyNoOtherCalls();
            _accountStatusRepositoryMock.VerifyNoOtherCalls();
        }

        #endregion
    }
}
