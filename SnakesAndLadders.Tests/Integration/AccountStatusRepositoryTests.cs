
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using System;
using System.Data.Entity.Infrastructure;

using System.Linq;
using Xunit;


namespace SnakesAndLadders.Tests.Integration
{
    // Esta colección desactiva el paralelismo para estas pruebas,
    // evitando deadlocks en la misma BD de pruebas.
    [CollectionDefinition("AccountStatusRepositoryTestsCollection", DisableParallelization = true)]
    public sealed class AccountStatusRepositoryTestsCollection
    {
    }

    [Collection("AccountStatusRepositoryTestsCollection")]
    public sealed class AccountStatusRepositoryTests : IntegrationTestBase
    {
        private const byte STATUS_ACTIVE_VALUE = 0x01;
        private const byte STATUS_INACTIVE_VALUE = 0x00;

        private const string EMAIL_PLAYER1 = "player1@test.com";
        private const string EMAIL_PLAYER2 = "player2@test.com";
        private const string EMAIL_MULTI1 = "multi1@test.com";
        private const string EMAIL_MULTI2 = "multi2@test.com";

        private const string EXPECTED_USERNAME_PREFIX = "deleted_";
        private const string EXPECTED_EMAIL_PREFIX = "deleted+";
        private const string EXPECTED_EMAIL_DOMAIN = "invalid.local";

        // TC-043 – userId inválido lanza ArgumentOutOfRangeException
        [Fact]
        public void TestSetUserAndAccountActiveStateWhenUserIdIsInvalidThrowsArgumentOutOfRange()
        {
            AccountStatusRepository repository = new AccountStatusRepository(CreateContext);

            Action action = () => repository.SetUserAndAccountActiveState(0, true);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        // TC-044 – activar usuario con una cuenta y una contraseña
        [Fact]
        public void TestSetUserAndAccountActiveStateActivatesUserAccountAndPassword()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = "Player1",
                    Nombre = "Player",
                    Apellidos = "One",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_INACTIVE_VALUE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;

                Cuenta account = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = EMAIL_PLAYER1,
                    Estado = new[] { STATUS_INACTIVE_VALUE }
                };

                db.Cuenta.Add(account);
                db.SaveChanges();

                Contrasenia password = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                    CuentaIdCuenta = account.IdCuenta,
                    Contrasenia1 = "Hash123",
                    Estado = new[] { STATUS_INACTIVE_VALUE },
                    FechaCreacion = DateTime.UtcNow
                };

                db.Contrasenia.Add(password);
                db.SaveChanges();
            }

            AccountStatusRepository repositoryToTest = new AccountStatusRepository(CreateContext);

            // Act
            repositoryToTest.SetUserAndAccountActiveState(userId, true);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Single(u => u.IdUsuario == userId);
                Cuenta accountRow = db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);
                Contrasenia passwordRow = db.Contrasenia.Single(p => p.UsuarioIdUsuario == userId);

                isOk =
                    userRow.Estado[0] == STATUS_ACTIVE_VALUE &&
                    accountRow.Estado[0] == STATUS_ACTIVE_VALUE &&
                    passwordRow.Estado[0] == STATUS_ACTIVE_VALUE &&
                    userRow.NombreUsuario == "Player1" &&
                    accountRow.Correo == EMAIL_PLAYER1;
            }

            Assert.True(isOk);
        }

        // TC-045 – desactivar usuario con cuenta y contraseña, renombrando y cambiando correo
        [Fact]
        public void TestSetUserAndAccountActiveStateDeactivatesUserAccountAndPasswordWithDeletedNaming()
        {
            int userId;
            string expectedUserName;
            string expectedEmail;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = "Player2",
                    Nombre = "Player",
                    Apellidos = "Two",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE_VALUE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;

                expectedUserName = $"{EXPECTED_USERNAME_PREFIX}{userId:D6}";
                expectedEmail = $"{EXPECTED_EMAIL_PREFIX}{userId:D6}@{EXPECTED_EMAIL_DOMAIN}";

                Cuenta account = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = EMAIL_PLAYER2,
                    Estado = new[] { STATUS_ACTIVE_VALUE }
                };

                db.Cuenta.Add(account);
                db.SaveChanges();

                Contrasenia password = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                    CuentaIdCuenta = account.IdCuenta,
                    Contrasenia1 = "Hash456",
                    Estado = new[] { STATUS_ACTIVE_VALUE },
                    FechaCreacion = DateTime.UtcNow
                };

                db.Contrasenia.Add(password);
                db.SaveChanges();
            }

            AccountStatusRepository repositoryToTest = new AccountStatusRepository(CreateContext);

            // Act
            repositoryToTest.SetUserAndAccountActiveState(userId, false);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Single(u => u.IdUsuario == userId);
                Cuenta accountRow = db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);
                Contrasenia passwordRow = db.Contrasenia.Single(p => p.UsuarioIdUsuario == userId);

                isOk =
                    userRow.NombreUsuario == expectedUserName &&
                    userRow.Estado[0] == STATUS_INACTIVE_VALUE &&
                    accountRow.Estado[0] == STATUS_INACTIVE_VALUE &&
                    passwordRow.Estado[0] == STATUS_INACTIVE_VALUE &&
                    accountRow.Correo == expectedEmail;
            }

            Assert.True(isOk);
        }

        // TC-046 – activar usuario sin cuentas ni contraseñas
        [Fact]
        public void TestSetUserAndAccountActiveStateActivatesUserWithoutAccountsOrPasswords()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = "SoloUser",
                    Nombre = "Solo",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_INACTIVE_VALUE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            AccountStatusRepository repositoryToTest = new AccountStatusRepository(CreateContext);

            // Act
            repositoryToTest.SetUserAndAccountActiveState(userId, true);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Single(u => u.IdUsuario == userId);
                bool hasAccounts = db.Cuenta.Any(c => c.UsuarioIdUsuario == userId);
                bool hasPasswords = db.Contrasenia.Any(p => p.UsuarioIdUsuario == userId);

                isOk =
                    userRow.Estado[0] == STATUS_ACTIVE_VALUE &&
                    !hasAccounts &&
                    !hasPasswords;
            }

            Assert.True(isOk);
        }

        // TC-047 – desactivar usuario sin cuentas ni contraseñas, renombrando
        [Fact]
        public void TestSetUserAndAccountActiveStateDeactivatesUserWithoutAccountsOrPasswords()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario user = new Usuario
                {
                    NombreUsuario = "SoloUser2",
                    Nombre = "Solo",
                    Apellidos = "User2",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = 0,
                    Estado = new[] { STATUS_ACTIVE_VALUE }
                };

                db.Usuario.Add(user);
                db.SaveChanges();
                userId = user.IdUsuario;
            }

            AccountStatusRepository repositoryToTest = new AccountStatusRepository(CreateContext);

            // Act
            repositoryToTest.SetUserAndAccountActiveState(userId, false);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Single(u => u.IdUsuario == userId);
                bool hasAccounts = db.Cuenta.Any(c => c.UsuarioIdUsuario == userId);
                bool hasPasswords = db.Contrasenia.Any(p => p.UsuarioIdUsuario == userId);

                string expectedUserName = $"{EXPECTED_USERNAME_PREFIX}{userId:D6}";

                isOk =
                    userRow.NombreUsuario == expectedUserName &&
                    userRow.Estado[0] == STATUS_INACTIVE_VALUE &&
                    !hasAccounts &&
                    !hasPasswords;
            }

            Assert.True(isOk);
        }

    }
}
