using System;
using System.Linq;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class AccountStatusRepositoryTests : IntegrationTestBase
    {
        private const int INITIAL_COINS = 0;

        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;
        private const int INVALID_USER_ID_BELOW_MIN =
            AccountStatusConstants.MIN_VALID_USER_ID - 1;

        [Fact]
        public void TestActivateUserAndAccountWithValidUserSetsAllStatusesToActive()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = new Usuario
                {
                    NombreUsuario = "ActivateTarget",
                    Nombre = "Activate",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_INACTIVE_VALUE
                    }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;

                Cuenta accountRow = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = "activate_target@test.com",
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_INACTIVE_VALUE
                    }
                };

                db.Cuenta.Add(accountRow);

                Contrasenia passwordRow = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                    Contrasenia1 = "Hash123",
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_INACTIVE_VALUE
                    },
                    FechaCreacion = DateTime.UtcNow
                };

                db.Contrasenia.Add(passwordRow);
                db.SaveChanges();
            }

            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            repository.ActivateUserAndAccount(userId);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Find(userId);
                Cuenta accountRow =
                    db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);
                Contrasenia passwordRow =
                    db.Contrasenia.Single(p => p.UsuarioIdUsuario == userId);

                byte expectedStatus = AccountStatusConstants.STATUS_ACTIVE_VALUE;

                isOk =
                    userRow != null &&
                    accountRow != null &&
                    passwordRow != null &&
                    userRow.Estado != null &&
                    accountRow.Estado != null &&
                    passwordRow.Estado != null &&
                    userRow.Estado.Length > 0 &&
                    accountRow.Estado.Length > 0 &&
                    passwordRow.Estado.Length > 0 &&
                    userRow.Estado[0] == expectedStatus &&
                    accountRow.Estado[0] == expectedStatus &&
                    passwordRow.Estado[0] == expectedStatus;
            }

            Assert.True(isOk);
        }

        [Fact]
        public void TestDeactivateUserAndAccountWithValidUserSetsAllStatusesToInactive()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = new Usuario
                {
                    NombreUsuario = "DeactivateTarget",
                    Nombre = "Deactivate",
                    Apellidos = "User",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_ACTIVE_VALUE
                    }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;

                Cuenta accountRow = new Cuenta
                {
                    UsuarioIdUsuario = userId,
                    Correo = "deactivate_target@test.com",
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_ACTIVE_VALUE
                    }
                };

                db.Cuenta.Add(accountRow);

                Contrasenia passwordRow = new Contrasenia
                {
                    UsuarioIdUsuario = userId,
                    Contrasenia1 = "Hash456",
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_ACTIVE_VALUE
                    },
                    FechaCreacion = DateTime.UtcNow
                };

                db.Contrasenia.Add(passwordRow);
                db.SaveChanges();
            }

            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            repository.DeactivateUserAndAccount(userId);

            bool isOk;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = db.Usuario.Find(userId);
                Cuenta accountRow =
                    db.Cuenta.Single(c => c.UsuarioIdUsuario == userId);
                Contrasenia passwordRow =
                    db.Contrasenia.Single(p => p.UsuarioIdUsuario == userId);

                byte expectedStatus = AccountStatusConstants.STATUS_INACTIVE_VALUE;

                isOk =
                    userRow != null &&
                    accountRow != null &&
                    passwordRow != null &&
                    userRow.Estado != null &&
                    accountRow.Estado != null &&
                    passwordRow.Estado != null &&
                    userRow.Estado.Length > 0 &&
                    accountRow.Estado.Length > 0 &&
                    passwordRow.Estado.Length > 0 &&
                    userRow.Estado[0] == expectedStatus &&
                    accountRow.Estado[0] == expectedStatus &&
                    passwordRow.Estado[0] == expectedStatus;
            }

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        [InlineData(INVALID_USER_ID_BELOW_MIN)]
        public void TestActivateUserAndAccountWithInvalidUserIdThrows(
            int userId)
        {
            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => repository.ActivateUserAndAccount(userId)) != null;

            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        [InlineData(INVALID_USER_ID_BELOW_MIN)]
        public void TestDeactivateUserAndAccountWithInvalidUserIdThrows(
            int userId)
        {
            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            bool throws =
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => repository.DeactivateUserAndAccount(userId)) != null;

            Assert.True(throws);
        }

        [Fact]
        public void TestActivateUserAndAccountWithUserWithoutAccountsDoesNotThrow()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = new Usuario
                {
                    NombreUsuario = "NoAccountsUser",
                    Nombre = "No",
                    Apellidos = "Accounts",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_INACTIVE_VALUE
                    }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;
            }

            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            bool isOk = true;

            try
            {
                repository.ActivateUserAndAccount(userId);
            }
            catch
            {
                isOk = false;
            }

            Assert.True(isOk);
        }

        [Fact]
        public void TestDeactivateUserAndAccountWithUserWithoutAccountsDoesNotThrow()
        {
            int userId;

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                Usuario userRow = new Usuario
                {
                    NombreUsuario = "NoAccountsDeactivate",
                    Nombre = "No",
                    Apellidos = "Accounts",
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = INITIAL_COINS,
                    Estado = new[]
                    {
                        AccountStatusConstants.STATUS_ACTIVE_VALUE
                    }
                };

                db.Usuario.Add(userRow);
                db.SaveChanges();
                userId = userRow.IdUsuario;
            }

            AccountStatusRepository repository =
                new AccountStatusRepository(CreateContext);

            bool isOk = true;

            try
            {
                repository.DeactivateUserAndAccount(userId);
            }
            catch
            {
                isOk = false;
            }

            Assert.True(isOk);
        }
    }
}
