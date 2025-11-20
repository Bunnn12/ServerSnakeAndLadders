using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Server.Helpers;

namespace ServerSnakesAndLadders
{
    /// <summary>
    /// Repository responsible for user, account and authentication persistence operations.
    /// </summary>
    public class AccountsRepository : IAccountsRepository
    {
        private const int EMAIL_MAX_LENGTH = 200;
        private const int USERNAME_MAX_LENGTH = 90;
        private const int PROFILE_DESCRIPTION_MAX_LENGTH = 510;
        private const int PASSWORD_HASH_MAX_LENGTH = 510;
        private const int PROFILE_PHOTO_ID_MAX_LENGTH = 5;

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int INITIAL_COINS = 0;
        private const byte STATUS_ACTIVE = 1;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountsRepository));

        public bool IsEmailRegistered(string email)
        {
            string normalizedEmail = Normalize(email, EMAIL_MAX_LENGTH);

            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                return dbContext.Cuenta
                    .AsNoTracking()
                    .Any(account => account.Correo == normalizedEmail);
            }
        }

        public bool IsUserNameTaken(string userName)
        {
            string normalizedUserName = Normalize(userName, USERNAME_MAX_LENGTH);

            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                return dbContext.Usuario
                    .AsNoTracking()
                    .Any(user => user.NombreUsuario == normalizedUserName);
            }
        }

        /// <summary>
        /// Creates a new user, account and password record in a single transaction.
        /// </summary>
        /// <param name="createAccountRequest">Data required to create the account.</param>
        /// <returns>New user identifier.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="SqlException">SQL errors are logged and rethrown.</exception>
        public int CreateUserWithAccountAndPassword(CreateAccountRequestDto createAccountRequest)
        {
            if (createAccountRequest == null)
            {
                throw new ArgumentNullException(nameof(createAccountRequest));
            }

            string userName = RequireParam(
                Normalize(createAccountRequest.Username, USERNAME_MAX_LENGTH),
                nameof(createAccountRequest.Username));

            string firstName = Normalize(createAccountRequest.FirstName, USERNAME_MAX_LENGTH);
            string lastName = Normalize(createAccountRequest.LastName, USERNAME_MAX_LENGTH);

            string profileDescription = Normalize(
                createAccountRequest.ProfileDescription,
                PROFILE_DESCRIPTION_MAX_LENGTH);

            string profilePhotoId = Normalize(
                createAccountRequest.ProfilePhotoId,
                PROFILE_PHOTO_ID_MAX_LENGTH);

            string email = RequireParam(
                Normalize(createAccountRequest.Email, EMAIL_MAX_LENGTH),
                nameof(createAccountRequest.Email));

            string passwordHash = RequireParam(
                Normalize(createAccountRequest.PasswordHash, PASSWORD_HASH_MAX_LENGTH),
                nameof(createAccountRequest.PasswordHash));

            using (var dbContext = new SnakeAndLaddersDBEntities1())
            using (var transaction = dbContext.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                try
                {
                    var userRow = new Usuario
                    {
                        NombreUsuario = userName,
                        Nombre = firstName,
                        Apellidos = lastName,
                        DescripcionPerfil = string.IsNullOrWhiteSpace(profileDescription)
                            ? null
                            : profileDescription,
                        FotoPerfil = string.IsNullOrWhiteSpace(profilePhotoId)
                            ? null
                            : profilePhotoId,
                        Monedas = INITIAL_COINS,
                        Estado = new[] { STATUS_ACTIVE }
                    };

                    dbContext.Usuario.Add(userRow);
                    dbContext.SaveChanges();

                    var accountRow = new Cuenta
                    {
                        UsuarioIdUsuario = userRow.IdUsuario,
                        Correo = email,
                        Estado = new[] { STATUS_ACTIVE }
                    };

                    dbContext.Cuenta.Add(accountRow);
                    dbContext.SaveChanges();

                    var passwordRow = new Contrasenia
                    {
                        UsuarioIdUsuario = userRow.IdUsuario,
                        Contrasenia1 = passwordHash,
                        Estado = new[] { STATUS_ACTIVE },
                        FechaCreacion = DateTime.UtcNow,
                        Cuenta = accountRow
                    };

                    dbContext.Contrasenia.Add(passwordRow);
                    dbContext.SaveChanges();

                    transaction.Commit();

                    return userRow.IdUsuario;
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Logger.Error("Error creating user with account and password (SQL).", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Error("Unexpected error creating user with account and password.", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Normalizes a string by trimming it and limiting it to the specified maximum length.
        /// Returns an empty string when the input is null or whitespace.
        /// </summary>
        private static string Normalize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();

            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed.Substring(0, maxLength);
        }

        private static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} is required.", paramName);
            }

            return value;
        }

        /// <summary>
        /// Retrieves authentication credentials by email or username identifier.
        /// Returns null when user or password cannot be resolved.
        /// </summary>
        public AuthCredentialsDto GetAuthByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            string trimmedIdentifier = identifier.Trim();

            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                var accountMatch = dbContext.Cuenta
                    .AsNoTracking()
                    .Where(account => account.Correo == identifier)
                    .Select(account => new { account.UsuarioIdUsuario })
                    .FirstOrDefault();

                int userId;

                if (accountMatch != null)
                {
                    userId = accountMatch.UsuarioIdUsuario;
                }
                else
                {
                    var userMatch = dbContext.Usuario
                        .AsNoTracking()
                        .Where(user => user.NombreUsuario == identifier)
                        .Select(user => new { user.IdUsuario })
                        .FirstOrDefault();

                    if (userMatch == null)
                    {
                        return null;
                    }

                    userId = userMatch.IdUsuario;
                }

                string lastPasswordHash = dbContext.Contrasenia
                    .AsNoTracking()
                    .Where(password => password.UsuarioIdUsuario == userId)
                    .OrderByDescending(password => password.FechaCreacion)
                    .Select(password => password.Contrasenia1)
                    .FirstOrDefault();

                if (lastPasswordHash == null)
                {
                    return null;
                }

                var userData = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user => user.IdUsuario == userId)
                    .Select(user => new
                    {
                        user.NombreUsuario,
                        user.FotoPerfil
                    })
                    .FirstOrDefault();

                if (userData == null)
                {
                    return null;
                }

                string normalizedAvatarId = AvatarIdHelper.MapFromDb(userData.FotoPerfil);

                return new AuthCredentialsDto
                {
                    UserId = userId,
                    PasswordHash = lastPasswordHash,
                    DisplayName = userData.NombreUsuario,
                    ProfilePhotoId = normalizedAvatarId
                };
            }
        }
    }
}