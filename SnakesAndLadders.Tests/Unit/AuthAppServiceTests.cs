using System;
using System.Collections.Generic;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    /*
    public sealed class AuthAppServiceTests
    {
        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IPlayerReportAppService> _playerReportAppServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IVerificationCodeStore> _verificationCodeStoreMock;

        private readonly AuthAppService _service;

        public AuthAppServiceTests()
        {
            _accountsRepositoryMock = new Mock<IAccountsRepository>(MockBehavior.Strict);
            _passwordHasherMock = new Mock<IPasswordHasher>(MockBehavior.Strict);
            _emailSenderMock = new Mock<IEmailSender>(MockBehavior.Strict);
            _playerReportAppServiceMock = new Mock<IPlayerReportAppService>(MockBehavior.Strict);
            _userRepositoryMock = new Mock<IUserRepository>(MockBehavior.Strict);
            _tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
            _verificationCodeStoreMock = new Mock<IVerificationCodeStore>(MockBehavior.Strict);

            _service = new chaAuthAppService(
                _accountsRepositoryMock.Object,
                _passwordHasherMock.Object,
                _emailSenderMock.Object,
                _playerReportAppServiceMock.Object,
                _userRepositoryMock.Object,
                _tokenServiceMock.Object,
                _verificationCodeStoreMock.Object);
        }

        #region RegisterUser

        [Fact]
        public void TestRegisterUserReturnsInvalidRequestWhenRegistrationIsNull()
        {
            AuthResult result = _service.RegisterUser(null);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Theory]
        [InlineData(null, "user@test.com", "Password1")]
        [InlineData("User", null, "Password1")]
        [InlineData("User", "user@test.com", null)]
        [InlineData(" ", "user@test.com", "Password1")]
        public void TestRegisterUserReturnsInvalidRequestWhenRegistrationHasMissingFields(
            string userName,
            string email,
            string password)
        {
            var registration = new RegistrationDto
            {
                UserName = userName,
                Email = email,
                Password = password
            };

            AuthResult result = _service.RegisterUser(registration);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Fact]
        public void TestRegisterUserReturnsEmailAlreadyExistsWhenEmailIsRegistered()
        {
            var registration = new RegistrationDto
            {
                UserName = "UserOne",
                Email = "user@test.com",
                Password = "Password1"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered("user@test.com"))
                .Returns(true);

            AuthResult result = _service.RegisterUser(registration);

            Assert.True(!result.Success && result.Code == "Auth.EmailAlreadyExists");
        }

        [Fact]
        public void TestRegisterUserReturnsUserNameAlreadyExistsWhenUserNameTaken()
        {
            var registration = new RegistrationDto
            {
                UserName = "UserOne",
                Email = "user@test.com",
                Password = "Password1"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered("user@test.com"))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken("UserOne"))
                .Returns(true);

            AuthResult result = _service.RegisterUser(registration);

            Assert.True(!result.Success && result.Code == "Auth.UserNameAlreadyExists");
        }

        [Fact]
        public void TestRegisterUserReturnsOkWhenCreationSucceeds()
        {
            var registration = new RegistrationDto
            {
                UserName = "UserOne",
                Email = "user@test.com",
                Password = "Password1",
                FirstName = "Test",
                LastName = "User"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered("user@test.com"))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken("UserOne"))
                .Returns(false);

            _passwordHasherMock
                .Setup(hasher => hasher.Hash("Password1"))
                .Returns("HASHED_PASSWORD");

            _accountsRepositoryMock
                .Setup(repo => repo.CreateUserWithAccountAndPassword(
                    It.Is<CreateAccountRequestDto>(dto =>
                        dto.Username == "UserOne"
                        && dto.Email == "user@test.com"
                        && dto.PasswordHash == "HASHED_PASSWORD")))
                .Returns(OperationResult<int>.Success(123));

            AuthResult result = _service.RegisterUser(registration);

            Assert.True(result.Success && result.Code == "Auth.Ok" && result.UserId == 123);
        }

        [Fact]
        public void TestRegisterUserReturnsServerErrorWhenCreationFails()
        {
            var registration = new RegistrationDto
            {
                UserName = "UserOne",
                Email = "user@test.com",
                Password = "Password1",
                FirstName = "Test",
                LastName = "User"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered("user@test.com"))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken("UserOne"))
                .Returns(false);

            _passwordHasherMock
                .Setup(hasher => hasher.Hash("Password1"))
                .Returns("HASHED_PASSWORD");

            _accountsRepositoryMock
                .Setup(repo => repo.CreateUserWithAccountAndPassword(It.IsAny<CreateAccountRequestDto>()))
                .Returns(OperationResult<int>.Failure("Error"));

            AuthResult result = _service.RegisterUser(registration);

            Assert.True(!result.Success
                        && result.Code == "Auth.ServerError"
                        && result.Meta != null
                        && result.Meta.TryGetValue("errorType", out string errorType)
                        && errorType == "SqlError");
        }

        #endregion

        #region Login

        [Fact]
        public void TestLoginReturnsInvalidRequestWhenRequestIsNull()
        {
            AuthResult result = _service.Login(null);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Theory]
        [InlineData(null, "Password1")]
        [InlineData("user@test.com", null)]
        [InlineData(" ", "Password1")]
        [InlineData("user@test.com", " ")]
        public void TestLoginReturnsInvalidRequestWhenRequiredFieldsMissing(
            string email,
            string password)
        {
            var request = new LoginDto
            {
                Email = email,
                Password = password
            };

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenAuthNotFound()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(null));

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidCredentials");
        }

        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenPasswordDoesNotMatch()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "HASHED",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1", "HASHED"))
                .Returns(false);

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidCredentials");
        }

        [Fact]
        public void TestLoginReturnsBannedWhenUserHasActiveBan()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "HASHED",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            var banInfo = new BanInfoDto
            {
                IsBanned = true,
                SanctionType = "TEMP_BAN",
                BanEndsAtUtc = DateTime.UtcNow.AddHours(1)
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1", "HASHED"))
                .Returns(true);

            _playerReportAppServiceMock
                .Setup(service => service.GetCurrentBan(10))
                .Returns(banInfo);

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success
                        && result.Code == "Auth.Banned"
                        && result.Meta != null
                        && result.Meta.ContainsKey("sanctionType"));
        }

        [Fact]
        public void TestLoginReturnsInvalidCredentialsWhenAccountNotFound()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "HASHED",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1", "HASHED"))
                .Returns(true);

            _playerReportAppServiceMock
                .Setup(service => service.GetCurrentBan(10))
                .Returns(new BanInfoDto
                {
                    IsBanned = false
                });

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(10))
                .Returns((AccountDto)null);

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidCredentials");
        }

        [Fact]
        public void TestLoginReturnsServerErrorWhenTokenIssueFails()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "HASHED",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            var account = new AccountDto
            {
                CurrentSkinId = "5",
                CurrentSkinUnlockedId = 7
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1", "HASHED"))
                .Returns(true);

            _playerReportAppServiceMock
                .Setup(service => service.GetCurrentBan(10))
                .Returns(new BanInfoDto
                {
                    IsBanned = false
                });

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(10))
                .Returns(account);

            _tokenServiceMock
                .Setup(service => service.IssueToken(10, It.IsAny<DateTime>()))
                .Throws(new Exception("Token error"));

            AuthResult result = _service.Login(request);

            Assert.True(!result.Success
                        && result.Code == "Auth.ServerError"
                        && result.Meta != null
                        && result.Meta.TryGetValue("errorType", out string errorType)
                        && errorType == "UnexpectedError");
        }

        [Fact]
        public void TestLoginReturnsOkWhenAllStepsSucceed()
        {
            var request = new LoginDto
            {
                Email = "user@test.com",
                Password = "Password1"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "HASHED",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            var account = new AccountDto
            {
                CurrentSkinId = "5",
                CurrentSkinUnlockedId = 7
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1", "HASHED"))
                .Returns(true);

            _playerReportAppServiceMock
                .Setup(service => service.GetCurrentBan(10))
                .Returns(new BanInfoDto
                {
                    IsBanned = false
                });

            _userRepositoryMock
                .Setup(repo => repo.GetByUserId(10))
                .Returns(account);

            _tokenServiceMock
                .Setup(service => service.IssueToken(10, It.IsAny<DateTime>()))
                .Returns("TOKEN-123");

            AuthResult result = _service.Login(request);

            Assert.True(result.Success
                        && result.Code == "Auth.Ok"
                        && result.Token == "TOKEN-123"
                        && result.CurrentSkinId == "5"
                        && result.CurrentSkinUnlockedId == 7);
        }

        #endregion

        #region RequestEmailVerification

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestRequestEmailVerificationReturnsEmailRequiredWhenEmailMissing(string email)
        {
            AuthResult result = _service.RequestEmailVerification(email);

            Assert.True(!result.Success && result.Code == "Auth.EmailRequired");
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsEmailAlreadyExistsWhenRegistered()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(true);

            AuthResult result = _service.RequestEmailVerification(email);

            Assert.True(!result.Success && result.Code == "Auth.EmailAlreadyExists");
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsThrottleWhenResendWindowNotElapsed()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(false);

            var entry = new VerificationCodeEntry
            {
                Code = "123456",
                LastSentUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out entry))
                .Returns(true);

            AuthResult result = _service.RequestEmailVerification(email);

            Assert.True(!result.Success && result.Code == "Auth.ThrottleWait");
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsOkWhenEmailIsValidAndNotRegistered()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(false);

            VerificationCodeEntry ignoredEntry;

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out ignoredEntry))
                .Returns(false);

            _verificationCodeStoreMock
                .Setup(store => store.SaveNewCode(
                    email,
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    email,
                    It.IsAny<string>()));

            AuthResult result = _service.RequestEmailVerification(email);

            Assert.True(result.Success && result.Code == "Auth.Ok");
        }

        #endregion

        #region ConfirmEmailVerification

        [Theory]
        [InlineData(null, "123456")]
        [InlineData("user@test.com", null)]
        [InlineData(" ", "123456")]
        [InlineData("user@test.com", " ")]
        public void TestConfirmEmailVerificationReturnsInvalidRequestWhenMissingData(
            string email,
            string code)
        {
            AuthResult result = _service.ConfirmEmailVerification(email, code);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsCodeNotRequestedWhenEntryNotFound()
        {
            const string email = "user@test.com";
            const string code = "123456";

            VerificationCodeEntry entry;

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out entry))
                .Returns(false);

            AuthResult result = _service.ConfirmEmailVerification(email, code);

            Assert.True(!result.Success && result.Code == "Auth.CodeNotRequested");
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsCodeExpiredWhenEntryExpired()
        {
            const string email = "user@test.com";
            const string code = "123456";

            var entry = new VerificationCodeEntry
            {
                Code = code,
                LastSentUtc = DateTime.UtcNow.AddMinutes(-10),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(-1)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove(email));

            AuthResult result = _service.ConfirmEmailVerification(email, code);

            Assert.True(!result.Success && result.Code == "Auth.CodeExpired");
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsOkWhenCodeMatches()
        {
            const string email = "user@test.com";
            const string code = "123456";

            var entry = new VerificationCodeEntry
            {
                Code = code,
                LastSentUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove(email));

            AuthResult result = _service.ConfirmEmailVerification(email, code);

            Assert.True(result.Success && result.Code == "Auth.Ok");
        }

        #endregion

        #region RequestPasswordChangeCode

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestRequestPasswordChangeCodeReturnsEmailRequiredWhenMissing(string email)
        {
            AuthResult result = _service.RequestPasswordChangeCode(email);

            Assert.True(!result.Success && result.Code == "Auth.EmailRequired");
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsEmailNotFoundWhenNotRegistered()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(false);

            AuthResult result = _service.RequestPasswordChangeCode(email);

            Assert.True(!result.Success && result.Code == "Auth.EmailNotFound");
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsThrottleWhenResendWindowNotElapsed()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(true);

            var entry = new VerificationCodeEntry
            {
                Code = "123456",
                LastSentUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out entry))
                .Returns(true);

            AuthResult result = _service.RequestPasswordChangeCode(email);

            Assert.True(!result.Success && result.Code == "Auth.ThrottleWait");
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsOkWhenEmailRegistered()
        {
            const string email = "user@test.com";

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(email))
                .Returns(true);

            VerificationCodeEntry ignoredEntry;

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(email, out ignoredEntry))
                .Returns(false);

            _verificationCodeStoreMock
                .Setup(store => store.SaveNewCode(
                    email,
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    email,
                    It.IsAny<string>()));

            AuthResult result = _service.RequestPasswordChangeCode(email);

            Assert.True(result.Success && result.Code == "Auth.Ok");
        }

        #endregion

        #region ChangePassword

        [Fact]
        public void TestChangePasswordReturnsInvalidRequestWhenRequestIsNull()
        {
            AuthResult result = _service.ChangePassword(null);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Theory]
        [InlineData(null, "Password1A", "123456")]
        [InlineData("user@test.com", null, "123456")]
        [InlineData("user@test.com", "Password1A", null)]
        [InlineData(" ", "Password1A", "123456")]
        public void TestChangePasswordReturnsInvalidRequestWhenMissingData(
            string email,
            string newPassword,
            string verificationCode)
        {
            var request = new ChangePasswordRequestDto
            {
                Email = email,
                NewPassword = newPassword,
                VerificationCode = verificationCode
            };

            AuthResult result = _service.ChangePassword(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidRequest");
        }

        [Fact]
        public void TestChangePasswordReturnsPasswordWeakWhenFormatInvalid()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@test.com",
                NewPassword = "weak",
                VerificationCode = "123456"
            };

            AuthResult result = _service.ChangePassword(request);

            Assert.True(!result.Success && result.Code == "Auth.PasswordWeak");
        }

        [Fact]
        public void TestChangePasswordReturnsInvalidCredentialsWhenUserNotFound()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@test.com",
                NewPassword = "Password1A",
                VerificationCode = "123456"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(null));

            AuthResult result = _service.ChangePassword(request);

            Assert.True(!result.Success && result.Code == "Auth.InvalidCredentials");
        }

        [Fact]
        public void TestChangePasswordReturnsCodeNotRequestedWhenVerificationEntryMissing()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@test.com",
                NewPassword = "Password1A",
                VerificationCode = "123456"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "OLD_HASH",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            VerificationCodeEntry entry;

            _verificationCodeStoreMock
                .Setup(store => store.TryGet("user@test.com", out entry))
                .Returns(false);

            AuthResult result = _service.ChangePassword(request);

            Assert.True(!result.Success && result.Code == "Auth.CodeNotRequested");
        }

        [Fact]
        public void TestChangePasswordReturnsPasswordReusedWhenNewMatchesHistory()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@test.com",
                NewPassword = "Password1A",
                VerificationCode = "123456"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "OLD_HASH",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            var entry = new VerificationCodeEntry
            {
                Code = "123456",
                LastSentUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet("user@test.com", out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove("user@test.com"));

            var history = new List<string> { "HASH_1" }.AsReadOnly();

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(10, 3))
                .Returns(OperationResult<IReadOnlyList<string>>.Success(history));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1A", "HASH_1"))
                .Returns(true);

            AuthResult result = _service.ChangePassword(request);

            Assert.True(!result.Success && result.Code == "Auth.PasswordReused");
        }

        [Fact]
        public void TestChangePasswordReturnsOkWhenPasswordChangedSuccessfully()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@test.com",
                NewPassword = "Password1A",
                VerificationCode = "123456"
            };

            var credentials = new AuthCredentialsDto
            {
                UserId = 10,
                PasswordHash = "OLD_HASH",
                DisplayName = "UserOne",
                ProfilePhotoId = "PHOTO"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier("user@test.com"))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            var entry = new VerificationCodeEntry
            {
                Code = "123456",
                LastSentUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5)
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet("user@test.com", out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove("user@test.com"));

            var history = new List<string> { "HASH_1" }.AsReadOnly();

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(10, 3))
                .Returns(OperationResult<IReadOnlyList<string>>.Success(history));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify("Password1A", "HASH_1"))
                .Returns(false);

            _passwordHasherMock
                .Setup(hasher => hasher.Hash("Password1A"))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(10, "NEW_HASH"))
                .Returns(OperationResult<bool>.Success(true));

            AuthResult result = _service.ChangePassword(request);

            Assert.True(result.Success && result.Code == "Auth.Ok" && result.UserId == 10);
        }

        #endregion

        #region GetUserIdFromToken

        [Fact]
        public void TestGetUserIdFromTokenReturnsValueFromTokenService()
        {
            _tokenServiceMock
                .Setup(service => service.GetUserIdFromToken("TOKEN-1"))
                .Returns(42);

            int userId = _service.GetUserIdFromToken("TOKEN-1");

            Assert.Equal(42, userId);
        }

        #endregion
    }
    */
}
