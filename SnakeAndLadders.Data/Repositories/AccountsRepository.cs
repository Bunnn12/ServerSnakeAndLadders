using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Server.Helpers;

namespace ServerSnakesAndLadders
{
    public sealed class AccountsRepository : IAccountsRepository
    {
        private const int EMAIL_MAX_LENGTH = 200;
        private const int USERNAME_MAX_LENGTH = 90;
        private const int PROFILE_DESC_MAX_LENGTH = 510;
        private const int PASSWORD_HASH_MAX_LENGTH = 510;
        private const int PROFILE_PHOTO_ID_MAX_LENGTH = 5;
        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int INITIAL_COINS = 0;
        private const byte STATUS_ACTIVE = 1;

        private const int MIN_VALID_USER_ID = 1;
        private const int MIN_PASSWORD_HISTORY_COUNT = 1;
        private const int STATUS_MIN_LENGTH = 1;
        private const int STATUS_ACTIVE_INDEX = 0;

        private const int EMPTY_COLLECTION_COUNT = 0;
        private const int MULTIPLE_ITEMS_MIN_COUNT = 2;
        private const int SUBSTRING_START_INDEX = 0;

        private const string ERROR_REQUEST_NULL = "Request cannot be null.";
        private const string ERROR_IDENTIFIER_REQUIRED = "Identifier is required.";
        private const string ERROR_INVALID_USERNAME_OR_PASSWORD = "Invalid username or password.";
        private const string ERROR_INVALID_CREDENTIALS = "Invalid credentials.";
        private const string ERROR_PROFILE_DATA_NOT_FOUND = "Profile data not found.";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        private const string ERROR_MAX_COUNT_POSITIVE = "maxCount must be positive.";
        private const string ERROR_PASSWORD_HASH_REQUIRED = "PasswordHash is required.";
        private const string ERROR_ACTIVE_ACCOUNT_NOT_FOUND = "Active account not found for user.";
        private const string ERROR_DATABASE_REGISTERING_USER = "Database error while registering user.";
        private const string ERROR_UNEXPECTED_REGISTERING_USER = "Unexpected error while registering user.";
        private const string ERROR_DATABASE_LOADING_PASSWORD_HISTORY = "Database error while loading password history.";
        private const string ERROR_UNEXPECTED_LOADING_PASSWORD_HISTORY = "Unexpected error while loading password history.";
        private const string ERROR_DATABASE_UPDATING_PASSWORD = "Database error while updating password.";
        private const string ERROR_UNEXPECTED_UPDATING_PASSWORD = "Unexpected error while updating password.";
        private const string ERROR_DATABASE_LOADING_EMAIL = "Database error while loading email.";
        private const string ERROR_UNEXPECTED_LOADING_EMAIL = "Unexpected error while loading email.";
        private const string ERROR_REQUIRED_TEMPLATE = "{0} is required.";

        private const string LOG_SQL_ERROR_CREATE_USER = "SQL error while creating user.";
        private const string LOG_UNEXPECTED_ERROR_CREATE_USER = "Unexpected error while creating user.";
        private const string LOG_INTEGRITY_USER_WITHOUT_PASSWORD = "Integrity issue: user {0} exists but has no password.";
        private const string LOG_SQL_ERROR_PASSWORD_HISTORY = "SQL error while loading password history.";
        private const string LOG_UNEXPECTED_ERROR_PASSWORD_HISTORY = "Unexpected error while loading password history.";
        private const string LOG_SQL_ERROR_INSERT_PASSWORD = "SQL error while inserting new password.";
        private const string LOG_UNEXPECTED_ERROR_INSERT_PASSWORD = "Unexpected error while inserting new password.";
        private const string LOG_SQL_ERROR_EMAIL_BY_USER_ID = "SQL error while loading email by user id.";
        private const string LOG_UNEXPECTED_ERROR_EMAIL_BY_USER_ID = "Unexpected error while loading email by user id.";
        private const string LOG_NO_ACTIVE_ACCOUNT_FOR_USER_CHANGING_PASSWORD = "No active account found for user when changing password. UserId={0}";
        private const string LOG_NO_ACTIVE_ACCOUNT_FOR_USER_GET_EMAIL = "GetEmailByUserId: no active account found for user. UserId={0}";
        private const string LOG_MULTIPLE_ACTIVE_ACCOUNTS_EMAIL = "Found {0} ACTIVE accounts with the same email {1}. Using the one with highest IdCuenta.";
        private const string LOG_MULTIPLE_ACTIVE_USERS_USERNAME = "Found {0} ACTIVE users with the same NombreUsuario {1}. Using the one with highest IdUsuario.";
        private const string LOG_MULTIPLE_ACTIVE_ACCOUNTS_FOR_USER = "Found {0} ACTIVE accounts for user {1}. Using the one with highest IdCuenta.";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountsRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public bool IsEmailRegistered(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string normalizedEmail = NormalizeString(email, EMAIL_MAX_LENGTH);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                List<Cuenta> accounts = context.Cuenta
                    .AsNoTracking()
                    .Where(account => account.Correo == normalizedEmail)
                    .ToList();

                return accounts.Any(account => IsActiveStatus(account.Estado));
            }
        }

        public bool IsUserNameTaken(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            string normalizedUserName = NormalizeString(userName, USERNAME_MAX_LENGTH);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                List<Usuario> users = context.Usuario
                    .AsNoTracking()
                    .Where(user => user.NombreUsuario == normalizedUserName)
                    .ToList();

                return users.Any(user => IsActiveStatus(user.Estado));
            }
        }

        public OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto request)
        {
            if (request == null)
            {
                return OperationResult<int>.Failure(ERROR_REQUEST_NULL);
            }

            try
            {
                (Usuario User, Cuenta Account, Contrasenia Password) userData = PrepareUserData(request);

                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        context.Usuario.Add(userData.User);
                        context.SaveChanges();

                        userData.Account.UsuarioIdUsuario = userData.User.IdUsuario;
                        context.Cuenta.Add(userData.Account);
                        context.SaveChanges();

                        userData.Password.UsuarioIdUsuario = userData.User.IdUsuario;
                        userData.Password.Cuenta = userData.Account;
                        context.Contrasenia.Add(userData.Password);
                        context.SaveChanges();

                        transaction.Commit();

                        return OperationResult<int>.Success(userData.User.IdUsuario);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        _logger.Error(LOG_SQL_ERROR_CREATE_USER, ex);
                        return OperationResult<int>.Failure(ERROR_DATABASE_REGISTERING_USER);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.Error(LOG_UNEXPECTED_ERROR_CREATE_USER, ex);
                        return OperationResult<int>.Failure(ERROR_UNEXPECTED_REGISTERING_USER);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                return OperationResult<int>.Failure(ex.Message);
            }
        }

        public OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return OperationResult<AuthCredentialsDto>.Failure(ERROR_IDENTIFIER_REQUIRED);
            }

            string trimmedIdentifier = identifier.Trim();

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                int? userId = GetUserIdByEmail(context, trimmedIdentifier)
                              ?? GetUserIdByUsername(context, trimmedIdentifier);

                if (!userId.HasValue)
                {
                    return OperationResult<AuthCredentialsDto>.Failure(ERROR_INVALID_USERNAME_OR_PASSWORD);
                }

                Usuario userEntity = context.Usuario
                    .AsNoTracking()
                    .SingleOrDefault(user => user.IdUsuario == userId.Value);

                if (userEntity == null || !IsActiveStatus(userEntity.Estado))
                {
                    return OperationResult<AuthCredentialsDto>.Failure(ERROR_INVALID_USERNAME_OR_PASSWORD);
                }

                string passwordHash = context.Contrasenia
                    .AsNoTracking()
                    .Where(password => password.UsuarioIdUsuario == userId.Value)
                    .OrderByDescending(password => password.FechaCreacion)
                    .Select(password => password.Contrasenia1)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    _logger.WarnFormat(
                        LOG_INTEGRITY_USER_WITHOUT_PASSWORD,
                        userId.Value);

                    return OperationResult<AuthCredentialsDto>.Failure(ERROR_INVALID_CREDENTIALS);
                }

                var userProfile = context.Usuario
                    .AsNoTracking()
                    .Where(user => user.IdUsuario == userId.Value)
                    .Select(user => new { user.NombreUsuario, user.FotoPerfil })
                    .FirstOrDefault();

                if (userProfile == null)
                {
                    return OperationResult<AuthCredentialsDto>.Failure(ERROR_PROFILE_DATA_NOT_FOUND);
                }

                var credentialsDto = new AuthCredentialsDto
                {
                    UserId = userId.Value,
                    PasswordHash = passwordHash,
                    DisplayName = userProfile.NombreUsuario,
                    ProfilePhotoId = AvatarIdHelper.MapFromDb(userProfile.FotoPerfil)
                };

                return OperationResult<AuthCredentialsDto>.Success(credentialsDto);
            }
        }

        public OperationResult<IReadOnlyList<string>> GetLastPasswordHashes(int userId, int maxCount)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                return OperationResult<IReadOnlyList<string>>.Failure(ERROR_USER_ID_POSITIVE);
            }

            if (maxCount < MIN_PASSWORD_HISTORY_COUNT)
            {
                return OperationResult<IReadOnlyList<string>>.Failure(ERROR_MAX_COUNT_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    List<string> hashes = context.Contrasenia
                        .AsNoTracking()
                        .Where(password => password.UsuarioIdUsuario == userId)
                        .OrderByDescending(password => password.FechaCreacion)
                        .Take(maxCount)
                        .Select(password => password.Contrasenia1)
                        .ToList();

                    IReadOnlyList<string> readOnlyHashes = hashes.AsReadOnly();

                    return OperationResult<IReadOnlyList<string>>.Success(readOnlyHashes);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(LOG_SQL_ERROR_PASSWORD_HISTORY, ex);
                return OperationResult<IReadOnlyList<string>>.Failure(ERROR_DATABASE_LOADING_PASSWORD_HISTORY);
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_UNEXPECTED_ERROR_PASSWORD_HISTORY, ex);
                return OperationResult<IReadOnlyList<string>>.Failure(ERROR_UNEXPECTED_LOADING_PASSWORD_HISTORY);
            }
        }

        public OperationResult<bool> AddPasswordHash(int userId, string passwordHash)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                return OperationResult<bool>.Failure(ERROR_USER_ID_POSITIVE);
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return OperationResult<bool>.Failure(ERROR_PASSWORD_HASH_REQUIRED);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    Cuenta activeAccount = GetActiveAccountForUser(context, userId);

                    if (activeAccount == null)
                    {
                        _logger.ErrorFormat(
                            LOG_NO_ACTIVE_ACCOUNT_FOR_USER_CHANGING_PASSWORD,
                            userId);

                        return OperationResult<bool>.Failure(ERROR_ACTIVE_ACCOUNT_NOT_FOUND);
                    }

                    var passwordEntity = new Contrasenia
                    {
                        UsuarioIdUsuario = userId,
                        CuentaIdCuenta = activeAccount.IdCuenta,
                        Contrasenia1 = passwordHash,
                        Estado = new[] { STATUS_ACTIVE },
                        FechaCreacion = DateTime.UtcNow
                    };

                    context.Contrasenia.Add(passwordEntity);
                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(LOG_SQL_ERROR_INSERT_PASSWORD, ex);
                return OperationResult<bool>.Failure(ERROR_DATABASE_UPDATING_PASSWORD);
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_UNEXPECTED_ERROR_INSERT_PASSWORD, ex);
                return OperationResult<bool>.Failure(ERROR_UNEXPECTED_UPDATING_PASSWORD);
            }
        }

        public OperationResult<string> GetEmailByUserId(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                return OperationResult<string>.Failure(ERROR_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    Cuenta activeAccount = GetActiveAccountForUser(context, userId);

                    if (activeAccount == null)
                    {
                        _logger.WarnFormat(
                            LOG_NO_ACTIVE_ACCOUNT_FOR_USER_GET_EMAIL,
                            userId);

                        return OperationResult<string>.Failure(ERROR_ACTIVE_ACCOUNT_NOT_FOUND);
                    }

                    string email = activeAccount.Correo ?? string.Empty;

                    return OperationResult<string>.Success(email);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(LOG_SQL_ERROR_EMAIL_BY_USER_ID, ex);
                return OperationResult<string>.Failure(ERROR_DATABASE_LOADING_EMAIL);
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_UNEXPECTED_ERROR_EMAIL_BY_USER_ID, ex);
                return OperationResult<string>.Failure(ERROR_UNEXPECTED_LOADING_EMAIL);
            }
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }

        private int? GetUserIdByEmail(SnakeAndLaddersDBEntities1 context, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            List<Cuenta> accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.Correo == email)
                .ToList();

            if (accounts.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            List<Cuenta> activeAccounts = accounts
                .Where(account => IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeAccounts.Count >= MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    LOG_MULTIPLE_ACTIVE_ACCOUNTS_EMAIL,
                    activeAccounts.Count,
                    email);
            }

            Cuenta selectedAccount = activeAccounts
                .OrderByDescending(account => account.IdCuenta)
                .First();

            return selectedAccount.UsuarioIdUsuario;
        }

        private int? GetUserIdByUsername(SnakeAndLaddersDBEntities1 context, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            List<Usuario> users = context.Usuario
                .AsNoTracking()
                .Where(user => user.NombreUsuario == username)
                .ToList();

            if (users.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            List<Usuario> activeUsers = users
                .Where(user => IsActiveStatus(user.Estado))
                .ToList();

            if (activeUsers.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeUsers.Count >= MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    LOG_MULTIPLE_ACTIVE_USERS_USERNAME,
                    activeUsers.Count,
                    username);
            }

            Usuario selectedUser = activeUsers
                .OrderByDescending(user => user.IdUsuario)
                .First();

            return selectedUser.IdUsuario;
        }

        private (Usuario User, Cuenta Account, Contrasenia Password) PrepareUserData(CreateAccountRequestDto request)
        {
            string userName = RequireParam(
                NormalizeString(request.Username, USERNAME_MAX_LENGTH),
                nameof(request.Username));

            string email = RequireParam(
                NormalizeString(request.Email, EMAIL_MAX_LENGTH),
                nameof(request.Email));

            string passwordHash = RequireParam(
                NormalizeString(request.PasswordHash, PASSWORD_HASH_MAX_LENGTH),
                nameof(request.PasswordHash));

            var user = new Usuario
            {
                NombreUsuario = userName,
                Nombre = NormalizeString(request.FirstName, USERNAME_MAX_LENGTH),
                Apellidos = NormalizeString(request.LastName, USERNAME_MAX_LENGTH),
                DescripcionPerfil = NormalizeString(request.ProfileDescription, PROFILE_DESC_MAX_LENGTH),
                FotoPerfil = NormalizeString(request.ProfilePhotoId, PROFILE_PHOTO_ID_MAX_LENGTH),
                Monedas = INITIAL_COINS,
                Estado = new[] { STATUS_ACTIVE }
            };

            var account = new Cuenta
            {
                Correo = email,
                Estado = new[] { STATUS_ACTIVE }
            };

            var password = new Contrasenia
            {
                Contrasenia1 = passwordHash,
                Estado = new[] { STATUS_ACTIVE },
                FechaCreacion = DateTime.UtcNow
            };

            return (user, account, password);
        }

        private Cuenta GetActiveAccountForUser(SnakeAndLaddersDBEntities1 context, int userId)
        {
            List<Cuenta> accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.UsuarioIdUsuario == userId)
                .ToList();

            if (accounts.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            List<Cuenta> activeAccounts = accounts
                .Where(account => IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeAccounts.Count >= MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    LOG_MULTIPLE_ACTIVE_ACCOUNTS_FOR_USER,
                    activeAccounts.Count,
                    userId);
            }

            Cuenta selectedAccount = activeAccounts
                .OrderByDescending(account => account.IdCuenta)
                .First();

            return selectedAccount;
        }

        private static string NormalizeString(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();

            if (trimmed.Length <= maxLength)
            {
                return trimmed;
            }

            return trimmed.Substring(SUBSTRING_START_INDEX, maxLength);
        }

        private static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string message = string.Format(ERROR_REQUIRED_TEMPLATE, paramName);
                throw new ArgumentException(message, paramName);
            }

            return value;
        }

        private static bool IsActiveStatus(byte[] status)
        {
            return status != null
                   && status.Length >= STATUS_MIN_LENGTH
                   && status[STATUS_ACTIVE_INDEX] == STATUS_ACTIVE;
        }
    }
}
