using System;
using System.Configuration;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class RegistrationAuthServiceTests
    {
        private const int NEW_USER_ID = 123;

        private const string EMAIL = "user@example.com";
        private const string PASSWORD = "Pass1234!";
        private const string USERNAME = "UserOne";
        private const string FIRST_NAME = "John";
        private const string LAST_NAME = "Doe";
        private const string PASSWORD_HASH = "HASHED_PASSWORD";

        private const string AUTH_CODE_OK = "Auth.Ok";
        private const string AUTH_CODE_INVALID_REQUEST = "Auth.InvalidRequest";
        private const string AUTH_CODE_EMAIL_ALREADY_EXISTS =
            "Auth.EmailAlreadyExists";
        private const string AUTH_CODE_USERNAME_ALREADY_EXISTS =
            "Auth.UserNameAlreadyExists";
        private const string AUTH_CODE_SERVER_ERROR = "Auth.ServerError";

        private const string META_KEY_ERROR_TYPE = "errorType";
        private const string ERROR_TYPE_SQL = "SqlError";
        private const string ERROR_TYPE_CONFIG = "ConfigError";
        private const string ERROR_TYPE_UNEXPECTED = "UnexpectedError";

        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;

        private readonly RegistrationAuthService _service;

        public RegistrationAuthServiceTests()
        {
            _accountsRepositoryMock =
                new Mock<IAccountsRepository>(MockBehavior.Strict);

            _passwordHasherMock =
                new Mock<IPasswordHasher>(MockBehavior.Strict);

            _service = new RegistrationAuthService(
                _accountsRepositoryMock.Object,
                _passwordHasherMock.Object);
        }


        [Fact]
        public void TestConstructorThrowsWhenAccountsRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new RegistrationAuthService(
                    null,
                    _passwordHasherMock.Object));

            bool isOk = ex.ParamName == "accountsRepository";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenPasswordHasherIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new RegistrationAuthService(
                    _accountsRepositoryMock.Object,
                    null));

            bool isOk = ex.ParamName == "passwordHasher";

            Assert.True(isOk);
        }



        [Fact]
        public void TestRegisterUserReturnsInvalidRequestWhenRegistrationIsNull()
        {
            AuthResult result = _service.RegisterUser(null);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_REQUEST;

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(null, PASSWORD, USERNAME)]
        [InlineData("", PASSWORD, USERNAME)]
        [InlineData("   ", PASSWORD, USERNAME)]
        [InlineData(EMAIL, null, USERNAME)]
        [InlineData(EMAIL, "", USERNAME)]
        [InlineData(EMAIL, "   ", USERNAME)]
        [InlineData(EMAIL, PASSWORD, null)]
        [InlineData(EMAIL, PASSWORD, "")]
        [InlineData(EMAIL, PASSWORD, "   ")]
        public void TestRegisterUserReturnsInvalidRequestWhenRequiredFieldsMissing(
            string email,
            string password,
            string userName)
        {
            var registration = new RegistrationDto
            {
                Email = email,
                Password = password,
                UserName = userName,
                FirstName = FIRST_NAME,
                LastName = LAST_NAME
            };

            AuthResult result = _service.RegisterUser(registration);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_INVALID_REQUEST;

            Assert.True(isOk);
        }


        [Fact]
        public void TestRegisterUserReturnsEmailAlreadyExistsWhenEmailIsRegistered()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Returns(true);

            AuthResult result = _service.RegisterUser(registration);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_EMAIL_ALREADY_EXISTS;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterUserReturnsUserNameAlreadyExistsWhenUserNameIsTaken()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken(USERNAME))
                .Returns(true);

            AuthResult result = _service.RegisterUser(registration);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_USERNAME_ALREADY_EXISTS;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterUserReturnsServerErrorUnexpectedWhenValidationFailsWithGenericException()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Throws(new Exception("Simulated generic error"));

            AuthResult result = _service.RegisterUser(registration);

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
        public void TestRegisterUserReturnsServerErrorConfigWhenValidationFailsWithConfig()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Throws(new ConfigurationErrorsException("config error"));

            AuthResult result = _service.RegisterUser(registration);

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
        public void TestRegisterUserReturnsServerErrorUnexpectedWhenValidationFailsWithUnexpected()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Throws(new InvalidOperationException("unexpected"));

            AuthResult result = _service.RegisterUser(registration);

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
        public void TestRegisterUserReturnsServerErrorSqlWhenCreateUserAccountFails()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken(USERNAME))
                .Returns(false);

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(PASSWORD))
                .Returns(PASSWORD_HASH);

            _accountsRepositoryMock
                .Setup(repo => repo.CreateUserWithAccountAndPassword(
                    It.IsAny<CreateAccountRequestDto>()))
                .Returns(OperationResult<int>.Failure("create failed"));

            AuthResult result = _service.RegisterUser(registration);

            bool isOk =
                result != null &&
                !result.Success &&
                result.Code == AUTH_CODE_SERVER_ERROR &&
                result.Meta != null &&
                result.Meta.ContainsKey(META_KEY_ERROR_TYPE) &&
                result.Meta[META_KEY_ERROR_TYPE] == ERROR_TYPE_SQL;

            Assert.True(isOk);
        }

        [Fact]
        public void TestRegisterUserReturnsOkWhenEverythingIsValid()
        {
            RegistrationDto registration = CreateValidRegistration();

            _accountsRepositoryMock
                .Setup(repo => repo.IsEmailRegistered(EMAIL))
                .Returns(false);

            _accountsRepositoryMock
                .Setup(repo => repo.IsUserNameTaken(USERNAME))
                .Returns(false);

            _passwordHasherMock
                .Setup(hasher => hasher.Hash(PASSWORD))
                .Returns(PASSWORD_HASH);

            _accountsRepositoryMock
                .Setup(repo => repo.CreateUserWithAccountAndPassword(
                    It.Is<CreateAccountRequestDto>(dto =>
                        dto.Email == EMAIL &&
                        dto.Username == USERNAME &&
                        dto.FirstName == FIRST_NAME &&
                        dto.LastName == LAST_NAME &&
                        dto.PasswordHash == PASSWORD_HASH)))
                .Returns(OperationResult<int>.Success(NEW_USER_ID));

            AuthResult result = _service.RegisterUser(registration);

            bool isOk =
                result != null &&
                result.Success &&
                result.Code == AUTH_CODE_OK &&
                result.UserId == NEW_USER_ID &&
                result.DisplayName == USERNAME;

            Assert.True(isOk);
        }

        private static RegistrationDto CreateValidRegistration()
        {
            return new RegistrationDto
            {
                Email = EMAIL,
                Password = PASSWORD,
                UserName = USERNAME,
                FirstName = FIRST_NAME,
                LastName = LAST_NAME
            };
        }

    }
}
