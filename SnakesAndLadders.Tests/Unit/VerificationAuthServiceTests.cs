using System;
using System.Configuration;
using System.Security.Cryptography;
using Moq;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class VerificationAuthServiceTests
    {
        private const string VALID_EMAIL = "user@example.com";
        private const string VALID_CODE = "123456";

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_EMAIL_REQUIRED = "Auth.EmailRequired";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS =
            "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_EMAIL_NOT_FOUND =
            "Auth.EmailNotFound";
        private const string AUTH_CODE_SERVER_ERROR = "Auth.ServerError";
        private const string AUTH_CODE_THROTTLE_WAIT =
            "Auth.ThrottleWait";
        private const string AUTH_CODE_EMAIL_SEND_FAILED =
            "Auth.EmailSendFailed";
        private const string AUTH_CODE_INVALID_REQUEST =
            "Auth.InvalidRequest";
        private const string AUTH_CODE_CODE_NOT_REQUESTED =
            "Auth.CodeNotRequested";
        private const string AUTH_CODE_CODE_EXPIRED =
            "Auth.CodeExpired";
        private const string AUTH_CODE_CODE_INVALID =
            "Auth.CodeInvalid";

        private const string META_KEY_SECONDS = "seconds";
        private const string META_KEY_REASON = "reason";
        private const string META_KEY_ERROR_TYPE = "errorType";

        private const string ERROR_TYPE_SQL = "SqlError";
        private const string ERROR_TYPE_CONFIG = "ConfigError";
        private const string ERROR_TYPE_CRYPTO = "CryptoError";
        private const string ERROR_TYPE_EMAIL_SEND = "EmailSendError";
        private const string ERROR_TYPE_UNEXPECTED = "UnexpectedError";

        private const int RESEND_WINDOW_SECONDS = 45;

        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IVerificationCodeStore> _codeStoreMock;

        private readonly VerificationAuthService _service;

        public VerificationAuthServiceTests()
        {
            _accountsRepositoryMock =
                new Mock<IAccountsRepository>(MockBehavior.Strict);

            _emailSenderMock =
                new Mock<IEmailSender>(MockBehavior.Strict);

            _codeStoreMock =
                new Mock<IVerificationCodeStore>(MockBehavior.Strict);

            _service = new VerificationAuthService(
                _accountsRepositoryMock.Object,
                _emailSenderMock.Object,
                _codeStoreMock.Object);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestRequestEmailVerificationReturnsEmailRequiredWhenEmailIsNullOrWhitespace(
            string email)
        {
            AuthResult result = _service.RequestEmailVerification(email);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_REQUIRED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsServerErrorConfigWhenRepositoryThrowsConfig()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Throws(new ConfigurationErrorsException("config"));

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

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
        public void TestRequestEmailVerificationReturnsServerErrorUnexpectedWhenRepositoryThrowsUnexpected()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

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
        public void TestRequestEmailVerificationReturnsEmailAlreadyExistsWhenEmailIsRegistered()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(true);

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_ALREADY_EXISTS;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsThrottleWaitWhenCodeRecentlySent()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(false);

            var entry = new VerificationCodeEntry
            {
                Code = "111111",
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow
                    .AddSeconds(-RESEND_WINDOW_SECONDS + 5),
                FailedAttempts = 0
            };

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out entry))
                .Returns(true);

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_THROTTLE_WAIT &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_SECONDS);

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsEmailSendFailedWhenCryptoErrorWhileSending()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(false);

            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            _codeStoreMock
                .Setup(store => store.SaveNewCode(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _codeStoreMock
                .Setup(store => store.Remove(VALID_EMAIL));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Throws(new CryptographicException("crypto"));

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_SEND_FAILED &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_REASON) &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_EMAIL_SEND;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsEmailSendFailedWhenEmailSenderThrows()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(false);

            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            _codeStoreMock
                .Setup(store => store.SaveNewCode(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _codeStoreMock
                .Setup(store => store.Remove(VALID_EMAIL));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Throws(new Exception("send error"));

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_SEND_FAILED &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_REASON) &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] ==
                    ERROR_TYPE_EMAIL_SEND;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestEmailVerificationReturnsOkWhenFlowIsSuccessful()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(false);

            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            _codeStoreMock
                .Setup(store => store.SaveNewCode(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    It.IsAny<string>(),
                    It.IsAny<string>()));

            AuthResult result = _service.RequestEmailVerification(VALID_EMAIL);

            bool isOk =
                result != null &&
                result.Success &&
                result.Code == AUTH_CODE_OK;

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(null, "123456")]
        [InlineData("", "123456")]
        [InlineData("   ", "123456")]
        [InlineData(VALID_EMAIL, null)]
        [InlineData(VALID_EMAIL, "")]
        [InlineData(VALID_EMAIL, "   ")]
        public void TestConfirmEmailVerificationReturnsInvalidRequestWhenEmailOrCodeInvalid(
            string email,
            string code)
        {
            AuthResult result =
                _service.ConfirmEmailVerification(email, code);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_REQUEST;

            Assert.True(isOk);
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsCodeNotRequestedWhenNoEntryExists()
        {
            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            AuthResult result =
                _service.ConfirmEmailVerification(VALID_EMAIL, VALID_CODE);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_NOT_REQUESTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsCodeExpiredWhenEntryExpired()
        {
            var entry = new VerificationCodeEntry
            {
                Code = VALID_CODE,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(-1),
                LastSentUtc = DateTime.UtcNow.AddMinutes(-5),
                FailedAttempts = 0
            };

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out entry))
                .Returns(true);

            _codeStoreMock
                .Setup(store => store.Remove(VALID_EMAIL));

            AuthResult result =
                _service.ConfirmEmailVerification(VALID_EMAIL, VALID_CODE);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_EXPIRED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsCodeInvalidWhenCodeDoesNotMatch()
        {
            var entry = new VerificationCodeEntry
            {
                Code = "000000",
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow,
                FailedAttempts = 0
            };

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out entry))
                .Returns(true);

            _codeStoreMock
                .Setup(store => store.RegisterFailedAttempt(
                    VALID_EMAIL,
                    entry))
                .Returns(entry);

            AuthResult result =
                _service.ConfirmEmailVerification(VALID_EMAIL, VALID_CODE);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_CODE_INVALID;

            Assert.True(isOk);
        }

        [Fact]
        public void TestConfirmEmailVerificationReturnsOkWhenCodeIsValidAndNotExpired()
        {
            var entry = new VerificationCodeEntry
            {
                Code = VALID_CODE,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow,
                FailedAttempts = 0
            };

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out entry))
                .Returns(true);

            _codeStoreMock
                .Setup(store => store.Remove(VALID_EMAIL));

            AuthResult result =
                _service.ConfirmEmailVerification(VALID_EMAIL, VALID_CODE);

            bool isOk =
                result != null &&
                result.Success &&
                result.Code == AUTH_CODE_OK;

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestRequestPasswordChangeCodeReturnsEmailRequiredWhenEmailInvalid(
            string email)
        {
            AuthResult result =
                _service.RequestPasswordChangeCode(email);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_REQUIRED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsServerErrorConfigWhenRepositoryThrowsConfig()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Throws(new ConfigurationErrorsException("config"));

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

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
        public void TestRequestPasswordChangeCodeReturnsServerErrorUnexpectedWhenRepositoryThrowsUnexpected()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] ==
                    ERROR_TYPE_UNEXPECTED;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsEmailNotFoundWhenEmailIsNotRegistered()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(false);

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_NOT_FOUND;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsThrottleWaitWhenRecentlySent()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(true);

            var entry = new VerificationCodeEntry
            {
                Code = "111111",
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                LastSentUtc = DateTime.UtcNow
                    .AddSeconds(-RESEND_WINDOW_SECONDS + 5),
                FailedAttempts = 0
            };

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out entry))
                .Returns(true);

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_THROTTLE_WAIT &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_SECONDS);

            Assert.True(isOk);
        }

        
        [Fact]
        public void TestRequestPasswordChangeCodeReturnsEmailSendFailedWhenEmailSenderThrows()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(true);

            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            _codeStoreMock
                .Setup(store => store.SaveNewCode(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _codeStoreMock
                .Setup(store => store.Remove(VALID_EMAIL));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Throws(new Exception("send error"));

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_SEND_FAILED &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_REASON) &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] ==
                    ERROR_TYPE_EMAIL_SEND;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRequestPasswordChangeCodeReturnsOkWhenFlowIsSuccessful()
        {
            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(
                    It.Is<string>(e => e == VALID_EMAIL)))
                .Returns(true);

            VerificationCodeEntry noEntry = null;

            _codeStoreMock
                .Setup(store => store.TryGet(VALID_EMAIL, out noEntry))
                .Returns(false);

            _codeStoreMock
                .Setup(store => store.SaveNewCode(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()));

            _emailSenderMock
                .Setup(sender => sender.SendVerificationCode(
                    It.IsAny<string>(),
                    It.IsAny<string>()));

            AuthResult result =
                _service.RequestPasswordChangeCode(VALID_EMAIL);

            bool isOk =
                result != null &&
                result.Success &&
                result.Code == AUTH_CODE_OK;

            Assert.True(isOk);
        }

    }
}
