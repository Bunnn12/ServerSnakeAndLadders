using System;
using System.Configuration;
using System.Security.Cryptography;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class AuthAppServiceTests : IDisposable
    {
        private const int VALID_USER_ID = 10;
        private const int INVALID_USER_ID = 0;

        private const string TRIMMED_TOKEN = "valid-token";
        private const string TOKEN_WITH_SPACES = "  valid-token  ";
        private const string NULL_TOKEN = null;
        private const string EMPTY_TOKEN = "";
        private const string WHITESPACE_TOKEN = "   ";


        private const string AUTH_CODE_OK = "Auth.Ok";

        private readonly Mock<IRegistrationAuthService> _registrationServiceMock;
        private readonly Mock<ILoginAuthService> _loginServiceMock;
        private readonly Mock<IVerificationAuthService> _verificationServiceMock;
        private readonly Mock<IPasswordChangeAuthService> _passwordChangeServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;

        private readonly AuthAppService _service;

        public AuthAppServiceTests()
        {
            _registrationServiceMock =
                new Mock<IRegistrationAuthService>(MockBehavior.Strict);

            _loginServiceMock =
                new Mock<ILoginAuthService>(MockBehavior.Strict);

            _verificationServiceMock =
                new Mock<IVerificationAuthService>(MockBehavior.Strict);

            _passwordChangeServiceMock =
                new Mock<IPasswordChangeAuthService>(MockBehavior.Strict);

            _tokenServiceMock =
                new Mock<ITokenService>(MockBehavior.Strict);

            _service = new AuthAppService(
                _registrationServiceMock.Object,
                _loginServiceMock.Object,
                _verificationServiceMock.Object,
                _passwordChangeServiceMock.Object,
                _tokenServiceMock.Object);
        }

        public void Dispose()
        {
            _registrationServiceMock.VerifyAll();
            _loginServiceMock.VerifyAll();
            _verificationServiceMock.VerifyAll();
            _passwordChangeServiceMock.VerifyAll();
            _tokenServiceMock.VerifyAll();
        }


        [Fact]
        public void TestConstructorThrowsWhenRegistrationServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new AuthAppService(
                    null,
                    _loginServiceMock.Object,
                    _verificationServiceMock.Object,
                    _passwordChangeServiceMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("registrationService", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenLoginServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new AuthAppService(
                    _registrationServiceMock.Object,
                    null,
                    _verificationServiceMock.Object,
                    _passwordChangeServiceMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("loginService", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenVerificationServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new AuthAppService(
                    _registrationServiceMock.Object,
                    _loginServiceMock.Object,
                    null,
                    _passwordChangeServiceMock.Object,
                    _tokenServiceMock.Object));

            Assert.Equal("verificationService", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenPasswordChangeServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new AuthAppService(
                    _registrationServiceMock.Object,
                    _loginServiceMock.Object,
                    _verificationServiceMock.Object,
                    null,
                    _tokenServiceMock.Object));

            Assert.Equal("passwordChangeService", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenTokenServiceIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new AuthAppService(
                    _registrationServiceMock.Object,
                    _loginServiceMock.Object,
                    _verificationServiceMock.Object,
                    _passwordChangeServiceMock.Object,
                    null));

            Assert.Equal("tokenService", ex.ParamName);
        }


        [Fact]
        public void TestRegisterUserDelegatesToRegistrationService()
        {
            var registration = new RegistrationDto
            {
                Email = "user@example.com",
                UserName = "UserOne",
                Password = "Pass1234"
            };

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _registrationServiceMock
                .Setup(svc => svc.RegisterUser(registration))
                .Returns(expectedResult);

            AuthResult result = _service.RegisterUser(registration);

            Assert.Same(expectedResult, result);
        }


        [Fact]
        public void TestLoginDelegatesToLoginService()
        {
            var request = new LoginDto
            {
                Email = "user@example.com",
                Password = "Pass1234"
            };

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _loginServiceMock
                .Setup(svc => svc.Login(request))
                .Returns(expectedResult);

            AuthResult result = _service.Login(request);

            Assert.Same(expectedResult, result);
        }


        [Fact]
        public void TestLogoutReturnsInvalidRequestWhenRequestIsNull()
        {
            AuthResult result = _service.Logout(null);

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(NULL_TOKEN)]
        [InlineData(EMPTY_TOKEN)]
        [InlineData(WHITESPACE_TOKEN)]
        public void TestLogoutReturnsInvalidRequestWhenTokenIsNullOrWhitespace(
            string token)
        {
            var request = new LogoutRequestDto
            {
                Token = token
            };

            AuthResult result = _service.Logout(request);

            Assert.False(result.Success);
        }

        [Fact]
        public void TestLogoutReturnsServerErrorWithConfigTypeWhenTokenServiceThrowsConfigurationError()
        {
            var request = new LogoutRequestDto
            {
                Token = TOKEN_WITH_SPACES
            };

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(TRIMMED_TOKEN))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.Logout(request);

            Assert.False(result.Success);
        }

        [Fact]
        public void TestLogoutReturnsServerErrorWithCryptoTypeWhenTokenServiceThrowsCryptographicError()
        {
            var request = new LogoutRequestDto
            {
                Token = TOKEN_WITH_SPACES
            };

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(TRIMMED_TOKEN))
                .Throws(new CryptographicException("crypto error"));

            AuthResult result = _service.Logout(request);

            Assert.False(result.Success);
    
        }

        [Fact]
        public void TestLogoutReturnsServerErrorWithUnexpectedTypeWhenTokenServiceThrowsUnexpectedError()
        {
            var request = new LogoutRequestDto
            {
                Token = TOKEN_WITH_SPACES
            };

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(TRIMMED_TOKEN))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.Logout(request);

            Assert.False(result.Success);

        }

        [Fact]
        public void TestLogoutReturnsInvalidRequestWhenTokenServiceResolvesToInvalidUserId()
        {
            var request = new LogoutRequestDto
            {
                Token = TOKEN_WITH_SPACES
            };

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(TRIMMED_TOKEN))
                .Returns(INVALID_USER_ID);

            AuthResult result = _service.Logout(request);

            Assert.False(result.Success);
        }

        [Fact]
        public void TestLogoutLogsOutAndReturnsOkWhenTokenIsValid()
        {
            var request = new LogoutRequestDto
            {
                Token = TOKEN_WITH_SPACES
            };

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(TRIMMED_TOKEN))
                .Returns(VALID_USER_ID);

            AuthResult result = _service.Logout(request);

            Assert.True(result.Success);
     
        }


        [Fact]
        public void TestRequestEmailVerificationDelegatesToVerificationService()
        {
            const string email = "user@example.com";

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _verificationServiceMock
                .Setup(svc => svc.RequestEmailVerification(email))
                .Returns(expectedResult);

            AuthResult result = _service.RequestEmailVerification(email);

            Assert.Same(expectedResult, result);
        }

        [Fact]
        public void TestConfirmEmailVerificationDelegatesToVerificationService()
        {
            const string email = "user@example.com";
            const string code = "123456";

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _verificationServiceMock
                .Setup(svc => svc.ConfirmEmailVerification(email, code))
                .Returns(expectedResult);

            AuthResult result = _service.ConfirmEmailVerification(email, code);

            Assert.Same(expectedResult, result);
        }

        [Fact]
        public void TestRequestPasswordChangeCodeDelegatesToVerificationService()
        {
            const string email = "user@example.com";

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _verificationServiceMock
                .Setup(svc => svc.RequestPasswordChangeCode(email))
                .Returns(expectedResult);

            AuthResult result = _service.RequestPasswordChangeCode(email);

            Assert.Same(expectedResult, result);
        }


        [Fact]
        public void TestChangePasswordDelegatesToPasswordChangeService()
        {
            var request = new ChangePasswordRequestDto
            {
                Email = "user@example.com",
                NewPassword = "Pass1234",
                VerificationCode = "123456"
            };

            var expectedResult = new AuthResult
            {
                Success = true,
                Code = AUTH_CODE_OK
            };

            _passwordChangeServiceMock
                .Setup(svc => svc.ChangePassword(request))
                .Returns(expectedResult);

            AuthResult result = _service.ChangePassword(request);

            Assert.Same(expectedResult, result);
        }


        [Fact]
        public void TestGetUserIdFromTokenDelegatesToTokenService()
        {
            const string token = "some-token";

            _tokenServiceMock
                .Setup(svc => svc.GetUserIdFromToken(token))
                .Returns(VALID_USER_ID);

            int result = _service.GetUserIdFromToken(token);

            Assert.Equal(VALID_USER_ID, result);
        }

    }
}
