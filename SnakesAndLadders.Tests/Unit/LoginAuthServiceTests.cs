using System;
using System.Configuration;
using System.Security.Cryptography;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class LoginAuthServiceTests
    {
        private const int VALID_USER_ID = 10;

        private const string VALID_EMAIL = "user@example.com";
        private const string VALID_PASSWORD = "Pass1234!";
        private const string VALID_PASSWORD_HASH = "HASHED";
        private const string VALID_DISPLAY_NAME = "User One";
        private const string VALID_PROFILE_PHOTO_ID = "PHOTO-1";
        private const string VALID_TOKEN = "valid-token";

        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IPlayerReportAppService> _playerReportServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;

        private readonly LoginAuthService _service;

        public LoginAuthServiceTests()
        {
            _accountsRepositoryMock =
                new Mock<IAccountsRepository>(MockBehavior.Strict);

            _passwordHasherMock =
                new Mock<IPasswordHasher>(MockBehavior.Strict);

            _playerReportServiceMock =
                new Mock<IPlayerReportAppService>(MockBehavior.Loose);

            _userRepositoryMock =
                new Mock<IUserRepository>(MockBehavior.Strict);

            _tokenServiceMock =
                new Mock<ITokenService>(MockBehavior.Strict);

            _service = new LoginAuthService(
                _accountsRepositoryMock.Object,
                _passwordHasherMock.Object,
                _playerReportServiceMock.Object,
                _userRepositoryMock.Object,
                _tokenServiceMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenAccountsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LoginAuthService(
                    null,
                    _passwordHasherMock.Object,
                    _playerReportServiceMock.Object,
                    _userRepositoryMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("accountsRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenPasswordHasherIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LoginAuthService(
                    _accountsRepositoryMock.Object,
                    null,
                    _playerReportServiceMock.Object,
                    _userRepositoryMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("passwordHasher", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenPlayerReportServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LoginAuthService(
                    _accountsRepositoryMock.Object,
                    _passwordHasherMock.Object,
                    null,
                    _userRepositoryMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("playerReportAppService", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenUserRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LoginAuthService(
                    _accountsRepositoryMock.Object,
                    _passwordHasherMock.Object,
                    _playerReportServiceMock.Object,
                    null,
                    _tokenServiceMock.Object));

            Assert.Equal("userRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenTokenServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LoginAuthService(
                    _accountsRepositoryMock.Object,
                    _passwordHasherMock.Object,
                    _playerReportServiceMock.Object,
                    _userRepositoryMock.Object,
                    null));

            Assert.Equal("tokenService", ex.ParamName);
        }


        [Fact]
        public void TestLoginReturnsInvalidRequestWhenRequestIsNull()
        {
            AuthResult result = _service.Login(null);

            Assert.False(result.Success);
           
        }

        [Theory]
        [InlineData(null, "pass")]
        [InlineData("", "pass")]
        [InlineData("   ", "pass")]
        [InlineData("user@example.com", null)]
        [InlineData("user@example.com", "")]
        [InlineData("user@example.com", "   ")]
        public void TestLoginReturnsInvalidRequestWhenEmailOrPasswordIsInvalid(
            string email,
            string password)
        {
            var request = new LoginDto
            {
                Email = email,
                Password = password
            };

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
       
        }


        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenRepositoryReturnsFailure()
        {
            var request = CreateValidLoginRequest();

            var authResult = OperationResult<AuthCredentialsDto>.Failure("error");

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Returns(authResult);

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);

        }

        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenRepositoryReturnsNullData()
        {
            var request = CreateValidLoginRequest();

            var authResult = OperationResult<AuthCredentialsDto>.Success(null);

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Returns(authResult);

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
   
        }

        [Fact]
        public void TestLoginReturnsServerErrorConfigWhenRepositoryThrowsConfigurationError()
        {
            var request = CreateValidLoginRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
      
        }

        [Fact]
        public void TestLoginReturnsServerErrorUnexpectedWhenRepositoryThrowsUnexpectedError()
        {
            var request = CreateValidLoginRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
         
        }

        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenPasswordDoesNotMatch()
        {
            var request = CreateValidLoginRequest();

            AuthCredentialsDto credentials = CreateAuthCredentials();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify(VALID_PASSWORD, VALID_PASSWORD_HASH))
                .Returns(false);

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
        
        }


        [Fact]
        public void TestLoginReturnsServerErrorConfigWhenBanCheckThrowsConfigurationError()
        {
            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsAndPasswordMatch();

            _playerReportServiceMock
                .Setup(svc => svc.GetCurrentBan(VALID_USER_ID))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
  
        }

        [Fact]
        public void TestLoginReturnsServerErrorUnexpectedWhenBanCheckThrowsUnexpectedError()
        {
            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsAndPasswordMatch();

            _playerReportServiceMock
                .Setup(svc => svc.GetCurrentBan(VALID_USER_ID))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);

        }


        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenAccountNotFound()
        {
            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordAndBanOk();

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns((AccountDto)null);

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
        }

        [Fact]
        public void TestLoginReturnsServerErrorConfigWhenAccountLoadThrowsConfigurationError()
        {
            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordAndBanOk();

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
         
        }

        [Fact]
        public void TestLoginReturnsServerErrorUnexpectedWhenAccountLoadThrowsUnexpectedError()
        {
            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordAndBanOk();

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
       
        }


        [Fact]
        public void TestLoginReturnsServerErrorConfigWhenTokenServiceThrowsConfigurationError()
        {
            ResetSession();

            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordBanAndAccount(out AccountDto account);

            _tokenServiceMock
                .Setup(svc => svc.IssueToken(VALID_USER_ID, It.IsAny<DateTime>()))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
         
        }

        [Fact]
        public void TestLoginReturnsServerErrorCryptoWhenTokenServiceThrowsCryptographicError()
        {
            ResetSession();

            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordBanAndAccount(out AccountDto account);

            _tokenServiceMock
                .Setup(svc => svc.IssueToken(VALID_USER_ID, It.IsAny<DateTime>()))
                .Throws(new CryptographicException("crypto error"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
        
        }

        [Fact]
        public void TestLoginReturnsServerErrorUnexpectedWhenTokenServiceThrowsUnexpectedError()
        {
            ResetSession();

            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordBanAndAccount(out AccountDto account);

            _tokenServiceMock
                .Setup(svc => svc.IssueToken(VALID_USER_ID, It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.Login(request);

            Assert.False(result.Success);
  
        }

        [Fact]
        public void TestLoginReturnsOkWhenEverythingIsValid()
        {
            ResetSession();

            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordBanAndAccount(out AccountDto account);

            _tokenServiceMock
                .Setup(svc => svc.IssueToken(VALID_USER_ID, It.IsAny<DateTime>()))
                .Returns(VALID_TOKEN);

            AuthResult result = _service.Login(request);

            Assert.True(result.Success);
           
        }

        [Fact]
        public void TestLoginReturnsSessionAlreadyActiveWhenSecondLoginWithSameUser()
        {
            ResetSession();

            var request = CreateValidLoginRequest();
            ConfigureValidCredentialsPasswordBanAndAccount(out AccountDto account);

            _tokenServiceMock
                .SetupSequence(svc => svc.IssueToken(VALID_USER_ID, It.IsAny<DateTime>()))
                .Returns(VALID_TOKEN)
                .Returns(VALID_TOKEN);

            AuthResult firstLogin = _service.Login(request);
            AuthResult secondLogin = _service.Login(request);

            Assert.False(secondLogin.Success);

        }

        private static LoginDto CreateValidLoginRequest()
        {
            return new LoginDto
            {
                Email = VALID_EMAIL,
                Password = VALID_PASSWORD
            };
        }

        private static AuthCredentialsDto CreateAuthCredentials()
        {
            return new AuthCredentialsDto
            {
                UserId = VALID_USER_ID,
                PasswordHash = VALID_PASSWORD_HASH,
                DisplayName = VALID_DISPLAY_NAME,
                ProfilePhotoId = VALID_PROFILE_PHOTO_ID
            };
        }

        private void ConfigureValidCredentialsAndPasswordMatch()
        {
            AuthCredentialsDto credentials = CreateAuthCredentials();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(VALID_EMAIL))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify(VALID_PASSWORD, VALID_PASSWORD_HASH))
                .Returns(true);
        }

        private void ConfigureValidCredentialsPasswordAndBanOk()
        {
            ConfigureValidCredentialsAndPasswordMatch();

        }

        private void ConfigureValidCredentialsPasswordBanAndAccount(
            out AccountDto account)
        {
            ConfigureValidCredentialsPasswordAndBanOk();

            account = new AccountDto
            {
                UserId = VALID_USER_ID,
                UserName = VALID_DISPLAY_NAME,
                CurrentSkinId = "5",
                CurrentSkinUnlockedId = 7
            };

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(VALID_USER_ID))
                .Returns(account);
        }

        private static void ResetSession()
        {
            InMemorySessionManager.Logout(VALID_USER_ID, VALID_TOKEN);
        }

    }
}
