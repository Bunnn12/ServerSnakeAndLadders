using System;
using System.Linq;
using ServerSnakesAndLadders;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public class AccountsRepositoryTests : IntegrationTestBase
    {
        private const int USERNAME_MAX_LENGTH = 90;
        private const int EMAIL_MAX_LENGTH = 200;
        private const int PROFILE_DESC_MAX_LENGTH = 510;
        private const int PASSWORD_HASH_MAX_LENGTH = 510;
        private const int PROFILE_PHOTO_ID_MAX_LENGTH = 5;
        private const int INITIAL_COINS = 0;
        private const byte STATUS_ACTIVE = 1;

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

            Assert.True(result.IsSuccess, $"Operation failed: {result.ErrorMessage}");
            Assert.True(result.Data > 0);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Find(result.Data);
                Assert.NotNull(userRow);
                Assert.Equal("JugadorPro", userRow.NombreUsuario);
                Assert.Equal("Juan", userRow.Nombre);
                Assert.Equal("Perez", userRow.Apellidos);
                Assert.Equal("Me gusta jugar", userRow.DescripcionPerfil);
                Assert.Equal("A0001", userRow.FotoPerfil);

                Cuenta accountRow = db.Cuenta.FirstOrDefault(c => c.UsuarioIdUsuario == result.Data);
                Assert.NotNull(accountRow);
                Assert.Equal("juan@game.com", accountRow.Correo);

                Contrasenia passwordRow = db.Contrasenia.FirstOrDefault(p => p.UsuarioIdUsuario == result.Data);
                Assert.NotNull(passwordRow);
                Assert.Equal("HashSeguro123", passwordRow.Contrasenia1);
            }
        }

        [Fact]
        public void TestCreateUserWhenRequestIsNullFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(null);

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Fact]
        public void TestCreateUserWhenUsernameIsMissingFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "   ",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "user@test.com",
                PasswordHash = "Hash",
                ProfileDescription = null,
                ProfilePhotoId = null
            };

            OperationResult<int> result = repository.CreateUserWithAccountAndPassword(requestDto);

            Assert.False(result.IsSuccess);
            Assert.Contains("UserName", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

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

            Assert.False(result.IsSuccess);
            Assert.Contains("Email", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

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

            Assert.False(result.IsSuccess);
            Assert.Contains("PasswordHash", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

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

        [Fact]
        public void TestIsEmailRegisteredWithNonExistingEmailFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool isRegistered = repository.IsEmailRegistered("not_found@test.com");

            Assert.False(isRegistered);
        }

        [Fact]
        public void TestIsEmailRegisteredWithNullOrWhitespaceFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool nullResult = repository.IsEmailRegistered(null);
            bool emptyResult = repository.IsEmailRegistered(string.Empty);
            bool whitespaceResult = repository.IsEmailRegistered("   ");

            Assert.False(nullResult);
            Assert.False(emptyResult);
            Assert.False(whitespaceResult);
        }

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

        [Fact]
        public void TestIsUserNameTakenWithNonExistingUserFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool isTaken = repository.IsUserNameTaken("GhostUser");

            Assert.False(isTaken);
        }

        [Fact]
        public void TestIsUserNameTakenWithNullOrWhitespaceFalse()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            bool nullResult = repository.IsUserNameTaken(null);
            bool emptyResult = repository.IsUserNameTaken(string.Empty);
            bool whitespaceResult = repository.IsUserNameTaken("   ");

            Assert.False(nullResult);
            Assert.False(emptyResult);
            Assert.False(whitespaceResult);
        }

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

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("LoginByEmail", result.Data.DisplayName);
            Assert.Equal("Secret123", result.Data.PasswordHash);
            Assert.Equal("A0001", result.Data.ProfilePhotoId);
        }


        [Fact]
        public void TestGetAuthByIdentifierWhenIdentifierIsNullFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier(null);

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Fact]
        public void TestGetAuthByIdentifierWhenUserDoesNotExistFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("ghost@test.com");

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

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
                    Monedas = 0,
                    Estado = new byte[] { 1 }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;

                Cuenta accountRow = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = "nopass@test.com",
                    Estado = new byte[] { 1 }
                };

                db.Cuenta.Add(accountRow);
                db.SaveChanges();
            }

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("nopass@test.com");

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


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

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("LoginTrimEmail", result.Data.DisplayName);
            Assert.Equal("Secret123", result.Data.PasswordHash);
            Assert.Equal("A0001", result.Data.ProfilePhotoId);
        }

        [Fact]
        public void TestGetAuthByIdentifierWhenIdentifierIsWhitespaceFailure()
        {
            AccountsRepository repository = new AccountsRepository(CreateContext);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("   ");

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

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
            Assert.True(createResult.IsSuccess, $"Operation failed: {createResult.ErrorMessage}");

            int userId = createResult.Data;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
               
                Cuenta accountRow = db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);

                Contrasenia newPassword = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                   
                    Cuenta = accountRow,
                    Contrasenia1 = "NewHash456",
                    Estado = new[] { (byte)1 },
                   
                    FechaCreacion = DateTime.UtcNow.AddSeconds(1)
                };

                db.Contrasenia.Add(newPassword);
                db.SaveChanges();
            }

            OperationResult<AuthCredentialsDto> authResult =
                repository.GetAuthByIdentifier("password_history@test.com");

            Assert.True(authResult.IsSuccess);
            Assert.NotNull(authResult.Data);
            Assert.Equal("NewHash456", authResult.Data.PasswordHash);
        }

    }
}
