using System;
using System.Linq;
using ServerSnakesAndLadders;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class AccountsRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE = 1;
        private const int INITIAL_COINS = 0;

        // TC-001
        [Fact]
        public void TestCreateUserWithValidDataPersistsUserAccountAndPassword()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto newUser = new CreateAccountRequestDto
            {
                Username = "JugadorPro",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan@game.com",
                PasswordHash = "HashSeguro123",
                ProfileDescription = "Me gusta jugar",
                ProfilePhotoId = "A0001"
            };

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(newUser);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Find(result.Data);
                Cuenta accountRow = db.Cuenta.FirstOrDefault(c => c.UsuarioIdUsuario == result.Data);
                Contrasenia passwordRow = db.Contrasenia.FirstOrDefault(p => p.UsuarioIdUsuario == result.Data);

                isOk =
                    result.IsSuccess &&
                    result.Data > 0 &&
                    userRow != null &&
                    userRow.NombreUsuario == "JugadorPro" &&
                    userRow.Nombre == "Juan" &&
                    userRow.Apellidos == "Perez" &&
                    userRow.DescripcionPerfil == "Me gusta jugar" &&
                    userRow.FotoPerfil == "A0001" &&
                    accountRow != null &&
                    accountRow.Correo == "juan@game.com" &&
                    passwordRow != null &&
                    passwordRow.Contrasenia1 == "HashSeguro123";
            }

            Assert.True(isOk);
        }

        // TC-002
        [Fact]
        public void TestCreateUserWhenRequestIsNullFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(null);

            bool isFailure = !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage);
            Assert.True(isFailure);
        }

        // TC-003
        [Fact]
        public void TestCreateUserWhenUsernameIsMissingFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "   ",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan@game.com",
                PasswordHash = "HashSeguro123",
                ProfileDescription = null,
                ProfilePhotoId = null
            };

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(requestDto);

            bool isFailure =
                !result.IsSuccess &&
                result.ErrorMessage?.IndexOf("Username", StringComparison.OrdinalIgnoreCase) >= 0;

            Assert.True(isFailure);
        }

        // TC-004
        [Fact]
        public void TestCreateUserWhenEmailIsMissingFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "UserX",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "   ",
                PasswordHash = "Hash",
                ProfileDescription = null,
                ProfilePhotoId = null
            };

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(requestDto);

            bool isFailure =
                !result.IsSuccess &&
                result.ErrorMessage?.IndexOf("Email", StringComparison.OrdinalIgnoreCase) >= 0;

            Assert.True(isFailure);
        }

        // TC-005
        [Fact]
        public void TestCreateUserWhenPasswordHashIsMissingFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "UserX",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "user@test.com",
                PasswordHash = "   ",
                ProfileDescription = null,
                ProfilePhotoId = null
            };

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(requestDto);

            bool isFailure =
                !result.IsSuccess &&
                result.ErrorMessage?.IndexOf("PasswordHash", StringComparison.OrdinalIgnoreCase) >= 0;

            Assert.True(isFailure);
        }

        // TC-006
        [Fact]
        public void TestIsEmailRegisteredWithExistingEmailTrue()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "EmailUser",
                FirstName = "F",
                LastName = "L",
                Email = "exists@test.com",
                PasswordHash = "Hash"
            };

            repository.CreateUserWithAccountAndPassword(requestDto);

            bool isRegistered = repository.IsEmailRegistered("exists@test.com");

            Assert.True(isRegistered);
        }

        // TC-007
        [Fact]
        public void TestIsEmailRegisteredWithNonExistingEmailFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool isRegistered = repository.IsEmailRegistered("not_found@test.com");

            Assert.True(!isRegistered);
        }

        // TC-008, TC-009, TC-010
        [Theory]
        [InlineData("")]
        [InlineData("       ")]
        [InlineData(null)]
        public void TestIsEmailRegisteredWithInvalidEmailReturnsFalse(string email)
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool result = repository.IsEmailRegistered(email);

            Assert.True(!result);
        }

        // TC-011
        [Fact]
        public void TestIsUserNameTakenWithExistingUserTrue()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "ExistingUser",
                FirstName = "F",
                LastName = "L",
                Email = "user_name@test.com",
                PasswordHash = "Hash"
            };

            repository.CreateUserWithAccountAndPassword(requestDto);

            bool isTaken = repository.IsUserNameTaken("ExistingUser");

            Assert.True(isTaken);
        }

        // TC-012
        [Fact]
        public void TestIsUserNameTakenWithNonExistingUserFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool isTaken = repository.IsUserNameTaken("GhostUser");

            Assert.True(!isTaken);
        }

        // TC-013, TC-014, TC-015
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TestIsUserNameTakenWithInvalidUserNameReturnsFalse(string username)
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool result = repository.IsUserNameTaken(username);

            Assert.True(!result);
        }

        // TC-016
        [Fact]
        public void TestGetAuthByIdentifierWithExistingEmailSuccess()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "LoginByEmail",
                FirstName = "F",
                LastName = "L",
                Email = "login_email@test.com",
                PasswordHash = "Secret123",
                ProfilePhotoId = "A0001"
            };

            repository.CreateUserWithAccountAndPassword(requestDto);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("login_email@test.com");

            bool isOk =
                result.IsSuccess &&
                result.Data != null &&
                result.Data.DisplayName == "LoginByEmail" &&
                result.Data.PasswordHash == "Secret123" &&
                result.Data.ProfilePhotoId == "A0001";

            Assert.True(isOk);
        }

        // TC-017
        [Fact]
        public void TestGetAuthByIdentifierWhenIdentifierIsNullFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier(null);

            bool isFailure = !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage);
            Assert.True(isFailure);
        }

        // TC-018
        [Fact]
        public void TestGetAuthByIdentifierWhenUserDoesNotExistFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("ghost@test.com");

            bool isFailure = !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage);
            Assert.True(isFailure);
        }

        // caso extra (similar a matriz): user sin contraseña
        [Fact]
        public void TestGetAuthByIdentifierWhenUserHasNoPasswordFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = new Usuario
                {
                    NombreUsuario = "NoPasswordUser",
                    Nombre = "No",
                    Apellidos = "Pass",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[] { STATUS_ACTIVE }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;

                Cuenta accountRow = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = "nopass@test.com",
                    Estado = new[] { STATUS_ACTIVE }
                };

                db.Cuenta.Add(accountRow);
                db.SaveChanges();
            }

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("nopass@test.com");

            bool isFailure = !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage);
            Assert.True(isFailure);
        }

        // TC-019
        [Fact]
        public void TestGetAuthByIdentifierTrimsIdentifierWhitespace()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "LoginTrimEmail",
                FirstName = "F",
                LastName = "L",
                Email = "trim_login@test.com",
                PasswordHash = "Secret123",
                ProfilePhotoId = "A0001"
            };

            repository.CreateUserWithAccountAndPassword(requestDto);

            OperationResult<AuthCredentialsDto> result =
                repository.GetAuthByIdentifier("   trim_login@test.com   ");

            bool isOk =
                result.IsSuccess &&
                result.Data != null &&
                result.Data.DisplayName == "LoginTrimEmail" &&
                result.Data.PasswordHash == "Secret123" &&
                result.Data.ProfilePhotoId == "A0001";

            Assert.True(isOk);
        }

        // TC-020
        [Fact]
        public void TestGetAuthByIdentifierWhenIdentifierIsWhitespaceFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("   ");

            bool isFailure = !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage);
            Assert.True(isFailure);
        }

        // TC-021
        [Fact]
        public void TestGetAuthByIdentifierReturnsMostRecentPasswordHash()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "PasswordHistoryUser",
                FirstName = "First",
                LastName = "Last",
                Email = "password_history@test.com",
                PasswordHash = "OldHash123",
                ProfilePhotoId = "A0001"
            };

            OperationResult<int> createResult = repository.CreateUserWithAccountAndPassword(requestDto);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                int userId = createResult.Data;
                Cuenta accountRow = db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);

                Contrasenia newPassword = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                    Cuenta = accountRow,
                    Contrasenia1 = "NewHash456",
                    Estado = new[] { STATUS_ACTIVE },
                    FechaCreacion = DateTime.UtcNow.AddSeconds(1)
                };

                db.Contrasenia.Add(newPassword);
                db.SaveChanges();
            }

            OperationResult<AuthCredentialsDto> authResult =
                repository.GetAuthByIdentifier("password_history@test.com");

            bool isOk =
                authResult.IsSuccess &&
                authResult.Data != null &&
                authResult.Data.PasswordHash == "NewHash456";

            Assert.True(isOk);
        }
    }
}
