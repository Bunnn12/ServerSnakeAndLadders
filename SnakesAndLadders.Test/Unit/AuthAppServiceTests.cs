using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public class AuthAppServiceTests
    {
        private const string TEST_SECRET = "SECRET_KEY_FOR_TESTS_1234567890";
        private const string TEST_TOKEN_MINUTES_KEY = "Auth:TokenMinutes";
        private const string TEST_SECRET_KEY = "Auth:Secret";

        private readonly Mock<IAccountsRepository> _accountsRepository;
        private readonly Mock<IPasswordHasher> _passwordHasher;
        private readonly Mock<IEmailSender> _emailSender;
        private readonly Mock<IPlayerReportAppService> _playerReportAppService;
        private readonly Mock<IUserRepository> _userRepository;

        private readonly AuthAppService _service;

        public AuthAppServiceTests()
        {
            if (ConfigurationManager.AppSettings[TEST_SECRET_KEY] == null)
            {
                ConfigurationManager.AppSettings[TEST_SECRET_KEY] = TEST_SECRET;
            }

            if (ConfigurationManager.AppSettings[TEST_TOKEN_MINUTES_KEY] == null)
            {
                ConfigurationManager.AppSettings[TEST_TOKEN_MINUTES_KEY] = "60";
            }

            _accountsRepository = new Mock<IAccountsRepository>();
            _passwordHasher = new Mock<IPasswordHasher>();
            _emailSender = new Mock<IEmailSender>();
            _playerReportAppService = new Mock<IPlayerReportAppService>();
            _userRepository = new Mock<IUserRepository>();

            _service = new AuthAppService(
                _accountsRepository.Object,
                _passwordHasher.Object,
                _emailSender.Object,
                _playerReportAppService.Object,
                _userRepository.Object);
        }


        [Fact]
        public void TestRegisterUserWhenRequestIsNullUsesInvalidRequestCode()
        {
            AuthResult result = _service.RegisterUser(null);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidRequest", result.Code);
        }

        [Fact]
        public void TestRegisterUserWhenRequiredFieldsAreEmptyUsesInvalidRequestCode()
        {
            RegistrationDto dto = new RegistrationDto
            {
                Email = "",
                Password = "",
                UserName = ""
            };

            AuthResult result = _service.RegisterUser(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidRequest", result.Code);
        }

        [Fact]
        public void TestRegisterUserWhenEmailAlreadyExistsUsesEmailAlreadyExistsCode()
        {
            RegistrationDto dto = new RegistrationDto
            {
                Email = "exists@test.com",
                UserName = "User1",
                Password = "123"
            };

            _accountsRepository.Setup(x => x.IsEmailRegistered(dto.Email)).Returns(true);

            AuthResult result = _service.RegisterUser(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.EmailAlreadyExists", result.Code);
            _accountsRepository.Verify(
                x => x.CreateUserWithAccountAndPassword(It.IsAny<CreateAccountRequestDto>()),
                Times.Never);
        }

        [Fact]
        public void TestRegisterUserWhenUserNameIsTakenUsesUserNameAlreadyExistsCode()
        {
            RegistrationDto dto = new RegistrationDto
            {
                Email = "new@test.com",
                UserName = "TakenUser",
                Password = "123"
            };

            _accountsRepository.Setup(x => x.IsEmailRegistered(dto.Email)).Returns(false);
            _accountsRepository.Setup(x => x.IsUserNameTaken(dto.UserName)).Returns(true);

            AuthResult result = _service.RegisterUser(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.UserNameAlreadyExists", result.Code);
        }

        [Fact]
        public void TestRegisterUserWhenRepositoryFailsUsesServerErrorCode()
        {
            RegistrationDto dto = new RegistrationDto
            {
                Email = "new@test.com",
                UserName = "User1",
                Password = "123"
            };

            _accountsRepository.Setup(x => x.IsEmailRegistered(dto.Email)).Returns(false);
            _accountsRepository.Setup(x => x.IsUserNameTaken(dto.UserName)).Returns(false);
            _passwordHasher.Setup(x => x.Hash(dto.Password)).Returns("hash_123");

            OperationResult<int> failureResult = OperationResult<int>.Failure("db error");
            _accountsRepository
                .Setup(x => x.CreateUserWithAccountAndPassword(It.IsAny<CreateAccountRequestDto>()))
                .Returns(failureResult);

            AuthResult result = _service.RegisterUser(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.ServerError", result.Code);
        }

        [Fact]
        public void TestRegisterUserWhenDataIsValidCreatesUserWithGeneratedId()
        {
            RegistrationDto dto = new RegistrationDto
            {
                Email = "new@test.com",
                UserName = "Hero",
                Password = "123"
            };

            _accountsRepository.Setup(x => x.IsEmailRegistered(dto.Email)).Returns(false);
            _accountsRepository.Setup(x => x.IsUserNameTaken(dto.UserName)).Returns(false);
            _passwordHasher.Setup(x => x.Hash(dto.Password)).Returns("hash_123");

            OperationResult<int> successResult = OperationResult<int>.Success(42);
            _accountsRepository
                .Setup(x => x.CreateUserWithAccountAndPassword(It.IsAny<CreateAccountRequestDto>()))
                .Returns(successResult);

            AuthResult result = _service.RegisterUser(dto);

            Assert.True(result.Success);
            Assert.Equal("Auth.Ok", result.Code);
            Assert.Equal(42, result.UserId);
            Assert.Equal("Hero", result.DisplayName);
        }

        [Fact]
        public void TestLoginWhenRequestIsNullUsesInvalidRequestCode()
        {
            AuthResult result = _service.Login(null);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidRequest", result.Code);
        }

        [Fact]
        public void TestLoginWhenEmailOrPasswordIsEmptyUsesInvalidRequestCode()
        {
            LoginDto dto = new LoginDto
            {
                Email = "",
                Password = ""
            };

            AuthResult result = _service.Login(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidRequest", result.Code);
        }

        [Fact]
        public void TestLoginWhenRepositoryIndicatesFailureUsesInvalidCredentialsCode()
        {
            LoginDto dto = new LoginDto
            {
                Email = "no@user.com",
                Password = "123"
            };

            OperationResult<AuthCredentialsDto> failure =
                OperationResult<AuthCredentialsDto>.Failure("not found");

            _accountsRepository.Setup(x => x.GetAuthByIdentifier(dto.Email)).Returns(failure);

            AuthResult result = _service.Login(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidCredentials", result.Code);
        }

        [Fact]
        public void TestLoginWhenPasswordIsIncorrectUsesInvalidCredentialsCode()
        {
            LoginDto dto = new LoginDto
            {
                Email = "user@test.com",
                Password = "wrong"
            };

            AuthCredentialsDto credentials = new AuthCredentialsDto
            {
                UserId = 1,
                PasswordHash = "correctHash"
            };

            _accountsRepository
                .Setup(x => x.GetAuthByIdentifier(dto.Email))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasher.Setup(x => x.Verify("wrong", "correctHash")).Returns(false);

            AuthResult result = _service.Login(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidCredentials", result.Code);
        }

        [Fact]
        public void TestLoginWhenBanServiceThrowsUsesServerErrorCode()
        {
            LoginDto dto = new LoginDto
            {
                Email = "user@test.com",
                Password = "123"
            };

            AuthCredentialsDto credentials = new AuthCredentialsDto
            {
                UserId = 1,
                PasswordHash = "hash"
            };

            _accountsRepository
                .Setup(x => x.GetAuthByIdentifier(dto.Email))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasher.Setup(x => x.Verify(dto.Password, "hash")).Returns(true);
            _playerReportAppService
                .Setup(x => x.GetCurrentBan(1))
                .Throws(new InvalidOperationException("boom"));

            AuthResult result = _service.Login(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.ServerError", result.Code);
        }

        [Fact]
        public void TestLoginWhenUserIsBannedUsesBannedCodeAndMeta()
        {
            LoginDto dto = new LoginDto
            {
                Email = "banned@test.com",
                Password = "123"
            };

            AuthCredentialsDto credentials = new AuthCredentialsDto
            {
                UserId = 5,
                PasswordHash = "hash"
            };

            _accountsRepository
                .Setup(x => x.GetAuthByIdentifier(It.IsAny<string>()))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            BanInfoDto banInfo = new BanInfoDto
            {
                IsBanned = true,
                SanctionType = "Permaban",
                BanEndsAtUtc = DateTime.UtcNow.AddDays(1)
            };

            _playerReportAppService.Setup(x => x.GetCurrentBan(5)).Returns(banInfo);

            AuthResult result = _service.Login(dto);

            Assert.False(result.Success);
            Assert.Equal("Auth.Banned", result.Code);
            Assert.Equal("Permaban", result.Meta["sanctionType"]);
            Assert.True(result.Meta.ContainsKey("banEndsAtUtc"));
        }

        [Fact]
        public void TestLoginSuccessGeneratesTokenAndLoadsCurrentSkin()
        {
            LoginDto dto = new LoginDto
            {
                Email = "ok@test.com",
                Password = "123"
            };

            AuthCredentialsDto credentials = new AuthCredentialsDto
            {
                UserId = 1,
                PasswordHash = "hash",
                DisplayName = "Player"
            };

            _accountsRepository
                .Setup(x => x.GetAuthByIdentifier(It.IsAny<string>()))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _playerReportAppService.Setup(x => x.GetCurrentBan(It.IsAny<int>())).Returns((BanInfoDto)null);

            AccountDto account = new AccountDto
            {
                CurrentSkinId = "Skin01",
                CurrentSkinUnlockedId = 99
            };

            _userRepository.Setup(x => x.GetByUserId(1)).Returns(account);

            AuthResult result = _service.Login(dto);

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.Equal("Skin01", result.CurrentSkinId);
            Assert.Equal(99, result.CurrentSkinUnlockedId);
            Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
        }

        [Fact]
        public void TestRequestEmailVerificationWhenEmailIsEmptyUsesEmailRequiredCode()
        {
            AuthResult result = _service.RequestEmailVerification("  ");

            Assert.False(result.Success);
            Assert.Equal("Auth.EmailRequired", result.Code);
        }

        [Fact]
        public void TestRequestEmailVerificationWhenEmailAlreadyRegisteredUsesEmailAlreadyExistsCode()
        {
            const string EMAIL = "registered@test.com";

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(true);

            AuthResult result = _service.RequestEmailVerification(EMAIL);

            Assert.False(result.Success);
            Assert.Equal("Auth.EmailAlreadyExists", result.Code);
            _emailSender.Verify(
                x => x.SendVerificationCode(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TestRequestEmailVerificationSuccessSendsVerificationEmail()
        {
            const string EMAIL = "new@test.com";

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(false);

            AuthResult result = _service.RequestEmailVerification(EMAIL);

            Assert.True(result.Success);
            _emailSender.Verify(
                x => x.SendVerificationCode(EMAIL, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void TestRequestEmailVerificationSecondRequestWithinThrottleUsesThrottleWaitCode()
        {
            const string EMAIL = "throttle@test.com";

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(false);

            AuthResult first = _service.RequestEmailVerification(EMAIL);
            AuthResult second = _service.RequestEmailVerification(EMAIL);

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Equal("Auth.ThrottleWait", second.Code);
            Assert.True(second.Meta.ContainsKey("seconds"));
        }

        [Fact]
        public void TestRequestEmailVerificationWhenSenderThrowsUsesEmailSendFailedCode()
        {
            const string EMAIL = "error@test.com";

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(false);
            _emailSender
                .Setup(x => x.SendVerificationCode(EMAIL, It.IsAny<string>()))
                .Throws(new InvalidOperationException("smtp error"));

            AuthResult result = _service.RequestEmailVerification(EMAIL);

            Assert.False(result.Success);
            Assert.Equal("Auth.EmailSendFailed", result.Code);
            Assert.Equal("InvalidOperationException", result.Meta["reason"]);
        }

        [Fact]
        public void TestConfirmEmailVerificationWhenRequestIsInvalidUsesInvalidRequestCode()
        {
            AuthResult result = _service.ConfirmEmailVerification("  ", "  ");

            Assert.False(result.Success);
            Assert.Equal("Auth.InvalidRequest", result.Code);
        }

        [Fact]
        public void TestConfirmEmailVerificationWhenCodeNotRequestedUsesCodeNotRequestedCode()
        {
            AuthResult result = _service.ConfirmEmailVerification("someone@test.com", "123456");

            Assert.False(result.Success);
            Assert.Equal("Auth.CodeNotRequested", result.Code);
        }

        [Fact]
        public void TestConfirmEmailVerificationWhenCodeDoesNotMatchUsesCodeInvalidCode()
        {
            const string EMAIL = "code_mismatch@test.com";
            string capturedCode = null;

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(false);
            _emailSender
                .Setup(x => x.SendVerificationCode(EMAIL, It.IsAny<string>()))
                .Callback<string, string>((e, c) => capturedCode = c);

            AuthResult requestResult = _service.RequestEmailVerification(EMAIL);
            Assert.True(requestResult.Success);
            Assert.False(string.IsNullOrWhiteSpace(capturedCode));

            AuthResult confirmResult = _service.ConfirmEmailVerification(EMAIL, "000000");

            Assert.False(confirmResult.Success);
            Assert.Equal("Auth.CodeInvalid", confirmResult.Code);
        }

        [Fact]
        public void TestConfirmEmailVerificationWhenCodeIsValidUsesOkCode()
        {
            const string EMAIL = "valid_code@test.com";
            string capturedCode = null;

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL)).Returns(false);
            _emailSender
                .Setup(x => x.SendVerificationCode(EMAIL, It.IsAny<string>()))
                .Callback<string, string>((e, c) => capturedCode = c);

            AuthResult requestResult = _service.RequestEmailVerification(EMAIL);
            Assert.True(requestResult.Success);
            Assert.False(string.IsNullOrWhiteSpace(capturedCode));

            AuthResult confirmResult = _service.ConfirmEmailVerification(EMAIL, capturedCode);

            Assert.True(confirmResult.Success);
            Assert.Equal("Auth.Ok", confirmResult.Code);
        }

        [Fact]
        public void TestGetUserIdFromTokenWhenTokenIsNullOrWhitespaceYieldsZero()
        {
            int userId = _service.GetUserIdFromToken("   ");

            Assert.Equal(0, userId);
        }

        [Fact]
        public void TestGetUserIdFromTokenWhenTokenIsPlainIntegerUsesCompatibilityPath()
        {
            int userId = _service.GetUserIdFromToken("7");

            Assert.Equal(7, userId);
        }

        [Fact]
        public void TestGetUserIdFromTokenWhenTokenIsValidEncodesAndDecodesUserId()
        {
            const int USER_ID = 25;
            DateTime expires = DateTime.UtcNow.AddMinutes(30);
            string token = CreateSignedToken(USER_ID, expires, TEST_SECRET);

            int userId = _service.GetUserIdFromToken(token);

            Assert.Equal(USER_ID, userId);
        }

        [Fact]
        public void TestGetUserIdFromTokenWhenTokenIsExpiredYieldsZero()
        {
            const int USER_ID = 25;
            DateTime expired = DateTime.UtcNow.AddMinutes(-5);
            string token = CreateSignedToken(USER_ID, expired, TEST_SECRET);

            int userId = _service.GetUserIdFromToken(token);

            Assert.Equal(0, userId);
        }

        [Fact]
        public void TestGetUserIdFromTokenWhenSignatureIsTamperedYieldsZero()
        {
            const int USER_ID = 25;
            DateTime expires = DateTime.UtcNow.AddMinutes(30);
            string token = CreateSignedToken(USER_ID, expires, TEST_SECRET);

            string raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            string[] parts = raw.Split('|');
            parts[2] = "00" + parts[2]; // tamper signature
            string tamperedRaw = string.Join("|", parts);
            string tamperedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(tamperedRaw));

            int userId = _service.GetUserIdFromToken(tamperedToken);

            Assert.Equal(0, userId);
        }
        private static string CreateSignedToken(int userId, DateTime expiresAtUtc, string secret)
        {
            long expUnix = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
            string payload = $"{userId}|{expUnix}";
            string signatureHex = ComputeHmacHex(secret, payload);
            string raw = $"{payload}|{signatureHex}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        private static string ComputeHmacHex(string secret, string data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                for (int index = 0; index < bytes.Length; index++)
                {
                    builder.Append(bytes[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [Fact]
        public void TestLoginWhenAccountLoadThrowsStillSucceedsWithoutSkin()
        {
            LoginDto dto = new LoginDto
            {
                Email = "user@test.com",
                Password = "123"
            };

            AuthCredentialsDto credentials = new AuthCredentialsDto
            {
                UserId = 1,
                PasswordHash = "hash",
                DisplayName = "Player"
            };

            _accountsRepository
                .Setup(x => x.GetAuthByIdentifier(It.IsAny<string>()))
                .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

            _passwordHasher.Setup(x => x.Verify(dto.Password, "hash")).Returns(true);
            _playerReportAppService
                .Setup(x => x.GetCurrentBan(It.IsAny<int>()))
                .Returns((BanInfoDto)null);
            _userRepository
                .Setup(x => x.GetByUserId(1))
                .Throws(new InvalidOperationException("profile error"));

            AuthResult result = _service.Login(dto);
            Assert.True(result.Success);
            Assert.Equal("Auth.Ok", result.Code);
            Assert.Equal(1, result.UserId);
            Assert.Null(result.CurrentSkinId);
            Assert.Null(result.CurrentSkinUnlockedId);
        }

        [Fact]
        public void TestLoginWhenTokenMinutesConfigIsInvalidUsesDefaultMinutes()
        {

            string originalValue = ConfigurationManager.AppSettings["Auth:TokenMinutes"];
            ConfigurationManager.AppSettings["Auth:TokenMinutes"] = "not-an-int";

            try
            {
                LoginDto dto = new LoginDto
                {
                    Email = "user@test.com",
                    Password = "123"
                };

                AuthCredentialsDto credentials = new AuthCredentialsDto
                {
                    UserId = 1,
                    PasswordHash = "hash",
                    DisplayName = "Player"
                };

                _accountsRepository
                    .Setup(x => x.GetAuthByIdentifier(It.IsAny<string>()))
                    .Returns(OperationResult<AuthCredentialsDto>.Success(credentials));

                _passwordHasher.Setup(x => x.Verify(dto.Password, "hash")).Returns(true);
                _playerReportAppService
                    .Setup(x => x.GetCurrentBan(It.IsAny<int>()))
                    .Returns((BanInfoDto)null);

                DateTime before = DateTime.UtcNow;
                AuthResult result = _service.Login(dto);

                Assert.True(result.Success);
                Assert.True(result.ExpiresAtUtc.HasValue);

                TimeSpan ttl = result.ExpiresAtUtc.Value - before;
                Assert.True(ttl.TotalMinutes > 1000);   
                Assert.True(ttl.TotalDays <= 8);        
            }
            finally
            {

                ConfigurationManager.AppSettings["Auth:TokenMinutes"] = originalValue;
            }
        }


        [Fact]
        public void TestRequestEmailVerificationThrottleIsPerEmailNotGlobal()
        {

            const string EMAIL_ONE = "first@test.com";
            const string EMAIL_TWO = "second@test.com";

            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL_ONE)).Returns(false);
            _accountsRepository.Setup(x => x.IsEmailRegistered(EMAIL_TWO)).Returns(false);

            AuthResult firstFirst = _service.RequestEmailVerification(EMAIL_ONE);
            AuthResult firstSecond = _service.RequestEmailVerification(EMAIL_TWO);

            Assert.True(firstFirst.Success);
            Assert.True(firstSecond.Success);

            _emailSender.Verify(
                x => x.SendVerificationCode(EMAIL_ONE, It.IsAny<string>()),
                Times.Once);

            _emailSender.Verify(
                x => x.SendVerificationCode(EMAIL_TWO, It.IsAny<string>()),
                Times.Once);
        }
    }
}
