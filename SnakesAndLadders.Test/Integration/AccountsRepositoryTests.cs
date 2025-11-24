using System;
using System.Linq;
using ServerSnakesAndLadders;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Test.Integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public class AccountsRepositoryTests : IntegrationTestBase
    {

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
            Assert.Contains("Username", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
        public void TestGetAuthByIdentifierWithExistingUserNameSuccess()
        {

            AccountsRepository repository = new AccountsRepository(CreateContext);

            CreateAccountRequestDto requestDto = new CreateAccountRequestDto
            {
                Username = "LoginByUser",
                FirstName = "F",
                LastName = "L",
                Email = "login_user@test.com",
                PasswordHash = "Secret123",
                ProfilePhotoId = "A0001"
            };

            repository.CreateUserWithAccountAndPassword(requestDto);

            OperationResult<AuthCredentialsDto> result = repository.GetAuthByIdentifier("LoginByUser");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("LoginByUser", result.Data.DisplayName);
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
    }
}
