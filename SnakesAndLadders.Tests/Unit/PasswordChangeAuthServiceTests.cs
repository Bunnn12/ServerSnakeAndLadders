using System;
using System.Collections.Generic;
using System.Configuration;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class PasswordChangeAuthServiceTests
    {
        private const int USER_ID = 10;

        private const string EMAIL = "user@example.com";
        private const string NEW_PASSWORD = "Pass1234!";
        private const string WEAK_PASSWORD = "abc";
        private const string VERIFICATION_CODE = "123456";

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_INVALID_REQUEST = "Auth.InvalidRequest";
        private const string AUTH_CODE_INVALID_CREDENTIALS =
            "Auth.InvalidCredentials";
        private const string AUTH_CODE_PASSWORD_WEAK = "Auth.PasswordWeak";
        private const string AUTH_CODE_PASSWORD_REUSED =
            "Auth.PasswordReused";
        private const string AUTH_CODE_SERVER_ERROR = "Auth.ServerError";
        private const string AUTH_CODE_CODE_NOT_REQUESTED =
            "Auth.CodeNotRequested";
        private const string AUTH_CODE_CODE_EXPIRED =
            "Auth.CodeExpired";
        private const string AUTH_CODE_CODE_INVALID =
            "Auth.CodeInvalid";

        private const string META_KEY_ERROR_TYPE = "errorType";

        private const string ERROR_TYPE_SQL = "SqlError";
        private const string ERROR_TYPE_CONFIG = "ConfigError";
        private const string ERROR_TYPE_UNEXPECTED = "UnexpectedError";

        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IVerificationCodeStore> _verificationCodeStoreMock;

        private readonly PasswordChangeAuthService _service;

        public PasswordChangeAuthServiceTests()
        {
            _accountsRepositoryMock =
                new Mock<IAccountsRepository>(MockBehavior.Strict);

            _passwordHasherMock =
                new Mock<IPasswordHasher>(MockBehavior.Strict);

            _verificationCodeStoreMock =
                new Mock<IVerificationCodeStore>(MockBehavior.Strict);

            _service = new PasswordChangeAuthService(
                _accountsRepositoryMock.Object,
                _passwordHasherMock.Object,
                _verificationCodeStoreMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenAccountsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new PasswordChangeAuthService(
                    null,
                    _passwordHasherMock.Object,
                    _verificationCodeStoreMock.Object));

            bool isOk = ex.ParamName == "accountsRepository";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenPasswordHasherIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new PasswordChangeAuthService(
                    _accountsRepositoryMock.Object,
                    null,
                    _verificationCodeStoreMock.Object));

            bool isOk = ex.ParamName == "passwordHasher";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenVerificationCodeStoreIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new PasswordChangeAuthService(
                    _accountsRepositoryMock.Object,
                    _passwordHasherMock.Object,
                    null));

            bool isOk = ex.ParamName == "verificationCodeStore";

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsInvalidRequestWhenRequestIsNull()
        {
            AuthResult result = _service.ChangePassword(null);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_REQUEST;

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(null, NEW_PASSWORD, VERIFICATION_CODE)]
        [InlineData("", NEW_PASSWORD, VERIFICATION_CODE)]
        [InlineData("   ", NEW_PASSWORD, VERIFICATION_CODE)]
        [InlineData(EMAIL, null, VERIFICATION_CODE)]
        [InlineData(EMAIL, "", VERIFICATION_CODE)]
        [InlineData(EMAIL, "   ", VERIFICATION_CODE)]
        [InlineData(EMAIL, NEW_PASSWORD, null)]
        [InlineData(EMAIL, NEW_PASSWORD, "")]
        [InlineData(EMAIL, NEW_PASSWORD, "   ")]
        public void TestChangePasswordReturnsInvalidRequestWhenFieldsMissing(
            string email,
            string newPassword,
            string code)
        {
            var request = new ChangePasswordRequestDto
            {
                Email = email,
                NewPassword = newPassword,
                VerificationCode = code
            };

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_REQUEST;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsPasswordWeakWhenNewPasswordIsWeak()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = EMAIL,
                NewPassword = WEAK_PASSWORD,
                VerificationCode = VERIFICATION_CODE
            };

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_PASSWORD_WEAK;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorConfigWhenLoadingUserThrowsConfig()
        {
            var request = CreateValidRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Throws(new ConfigurationErrorsException("config"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_CONFIG;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorUnexpectedWhenLoadingUserThrowsUnexpected()
        {
            var request = CreateValidRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_UNEXPECTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsInvalidCredentialsWhenAuthResultIsFailure()
        {
            var request = CreateValidRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Failure("error"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_CREDENTIALS;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsInvalidCredentialsWhenAuthDataIsNull()
        {
            var request = CreateValidRequest();

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Success(null));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_CREDENTIALS;

            Assert.True(isOk);
        }


        [Fact]
        public void TestChangePasswordReturnsCodeNotRequestedWhenNoVerificationEntry()
        {
            var request = CreateValidRequest();

            var authCredentials = new AuthCredentialsDto
            {
                UserId = USER_ID
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Success(
                    authCredentials));

            VerificationCodeEntry noEntry = null;

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(
                    It.Is<string>(e => e == EMAIL),
                    out noEntry))
                .Returns(false);

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_NOT_REQUESTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsCodeExpiredWhenVerificationEntryExpired()
        {
            var request = CreateValidRequest();

            var authCredentials = new AuthCredentialsDto
            {
                UserId = USER_ID
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Success(
                    authCredentials));

            var entry = new VerificationCodeEntry
            {
                Code = VERIFICATION_CODE,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(-1),
                LastSentUtc = DateTime.UtcNow.AddMinutes(-5),
                FailedAttempts = 0
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(
                    It.Is<string>(e => e == EMAIL),
                    out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove(
                    It.Is<string>(e => e == EMAIL)));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_EXPIRED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsCodeInvalidWhenVerificationCodeDoesNotMatch()
        {
            var request = CreateValidRequest();

            var authCredentials = new AuthCredentialsDto
            {
                UserId = USER_ID
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Success(
                    authCredentials));

            var entry = new VerificationCodeEntry
            {
                Code = "000000",
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow,
                FailedAttempts = 0
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(
                    It.Is<string>(e => e == EMAIL),
                    out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.RegisterFailedAttempt(
                    It.Is<string>(e => e == EMAIL),
                    entry))
                .Returns(entry);

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_INVALID;

            Assert.True(isOk);
        }


        [Fact]
        public void TestChangePasswordReturnsServerErrorConfigWhenLoadingHistoryThrowsConfig()
        {
            var request = CreateValidRequest();

            ConfigureValidUserAndCode();

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(
                    USER_ID,
                    It.IsAny<int>()))
                .Throws(new ConfigurationErrorsException("config"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_CONFIG;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorUnexpectedWhenLoadingHistoryThrowsUnexpected()
        {
            var request = CreateValidRequest();

            ConfigureValidUserAndCode();

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(
                    USER_ID,
                    It.IsAny<int>()))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_UNEXPECTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorWhenHistoryIsEmpty()
        {
            var request = CreateValidRequest();

            ConfigureValidUserAndCode();

            var emptyHistory =
                OperationResult<IReadOnlyList<string>>.Success(
                    (IReadOnlyList<string>)new List<string>());

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(
                    USER_ID,
                    It.IsAny<int>()))
                .Returns(emptyHistory);

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsPasswordReusedWhenNewPasswordMatchesHistory()
        {
            var request = CreateValidRequest();

            ConfigureValidUserAndCode();

            var history = new List<string>
            {
                "OLD_HASH"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(
                    USER_ID,
                    It.IsAny<int>()))
                .Returns(OperationResult<IReadOnlyList<string>>.Success(
                    history));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify(
                    NEW_PASSWORD,
                    "OLD_HASH"))
                .Returns(true);

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_PASSWORD_REUSED;

            Assert.True(isOk);
        }


        [Fact]
        public void TestChangePasswordReturnsServerErrorConfigWhenPersistingPasswordThrowsConfig()
        {
            var request = CreateValidRequest();

            ConfigureValidUserCodeAndHistoryNoReuse();

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(NEW_PASSWORD))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(
                    USER_ID,
                    "NEW_HASH"))
                .Throws(new ConfigurationErrorsException("config"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_CONFIG;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorUnexpectedWhenPersistingPasswordThrowsUnexpected()
        {
            var request = CreateValidRequest();

            ConfigureValidUserCodeAndHistoryNoReuse();

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(NEW_PASSWORD))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(
                    USER_ID,
                    "NEW_HASH"))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_UNEXPECTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorWhenAddPasswordHashResultIsFailure()
        {
            var request = CreateValidRequest();

            ConfigureValidUserCodeAndHistoryNoReuse();

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(NEW_PASSWORD))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(
                    USER_ID,
                    "NEW_HASH"))
                .Returns(OperationResult<bool>.Failure("error"));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsServerErrorWhenAddPasswordHashReturnsFalse()
        {
            var request = CreateValidRequest();

            ConfigureValidUserCodeAndHistoryNoReuse();

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(NEW_PASSWORD))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(
                    USER_ID,
                    "NEW_HASH"))
                .Returns(OperationResult<bool>.Success(false));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR;

            Assert.True(isOk);
        }

        [Fact]
        public void TestChangePasswordReturnsOkWhenFlowIsSuccessful()
        {
            var request = CreateValidRequest();

            ConfigureValidUserCodeAndHistoryNoReuse();

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(NEW_PASSWORD))
                .Returns("NEW_HASH");

            _accountsRepositoryMock
                .Setup(repo => repo.AddPasswordHash(
                    USER_ID,
                    "NEW_HASH"))
                .Returns(OperationResult<bool>.Success(true));

            AuthResult result = _service.ChangePassword(request);

            bool isOk =
                result != null &&
                result.Success &&
                result.Code == AUTH_CODE_OK &&
                result.UserId == USER_ID;

            Assert.True(isOk);
        }

        private static ChangePasswordRequestDto CreateValidRequest()
        {
            return new ChangePasswordRequestDto
            {
                Email = EMAIL,
                NewPassword = NEW_PASSWORD,
                VerificationCode = VERIFICATION_CODE
            };
        }

        private void ConfigureValidUserAndCode()
        {
            var authCredentials = new AuthCredentialsDto
            {
                UserId = USER_ID
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetAuthByIdentifier(
                    It.Is<string>(e => e == EMAIL)))
                .Returns(OperationResult<AuthCredentialsDto>.Success(
                    authCredentials));

            var entry = new VerificationCodeEntry
            {
                Code = VERIFICATION_CODE,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow,
                FailedAttempts = 0
            };

            _verificationCodeStoreMock
                .Setup(store => store.TryGet(
                    It.Is<string>(e => e == EMAIL),
                    out entry))
                .Returns(true);

            _verificationCodeStoreMock
                .Setup(store => store.Remove(
                    It.Is<string>(e => e == EMAIL)));
        }

        private void ConfigureValidUserCodeAndHistoryNoReuse()
        {
            ConfigureValidUserAndCode();

            var history = new List<string>
            {
                "OTHER_HASH"
            };

            _accountsRepositoryMock
                .Setup(repo => repo.GetLastPasswordHashes(
                    USER_ID,
                    It.IsAny<int>()))
                .Returns(OperationResult<IReadOnlyList<string>>.Success(
                    history));

            _passwordHasherMock
                .Setup(hasher => hasher.Verify(
                    NEW_PASSWORD,
                    "OTHER_HASH"))
                .Returns(false);
        }

    }
}
    