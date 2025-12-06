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
    public class AccountsRepository : IAccountsRepository
    {
        private const int EMAIL_MAX_LENGTH = 200;
        private const int USERNAME_MAX_LENGTH = 90;
        private const int PROFILE_DESC_MAX_LENGTH = 510;
        private const int PASSWORD_HASH_MAX_LENGTH = 510;
        private const int PROFILE_PHOTO_ID_MAX_LENGTH = 5;
        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int INITIAL_COINS = 0;
        private const byte STATUS_ACTIVE = 1;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountsRepository));

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

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                var accounts = context.Cuenta
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

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                var users = context.Usuario
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
                return OperationResult<int>.Failure("Request cannot be null.");
            }

            try
            {
                var userData = PrepareUserData(request);

                using (var context = _contextFactory())
                using (var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
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
                        Logger.Error("SQL error while creating user.", ex);
                        return OperationResult<int>.Failure("Database error while registering user.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Unexpected error while creating user.", ex);
                        return OperationResult<int>.Failure("Unexpected error while registering user.");
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
                return OperationResult<AuthCredentialsDto>.Failure("Identifier is required.");
            }

            string trimmedIdentifier = identifier.Trim();

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                int? userId = GetUserIdByEmail(context, trimmedIdentifier)
                              ?? GetUserIdByUsername(context, trimmedIdentifier);

                if (!userId.HasValue)
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Invalid username or password.");
                }

                var userEntity = context.Usuario
                    .AsNoTracking()
                    .SingleOrDefault(user => user.IdUsuario == userId.Value);

                if (userEntity == null || !IsActiveStatus(userEntity.Estado))
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Invalid username or password.");
                }

                string passwordHash = context.Contrasenia
                    .AsNoTracking()
                    .Where(password => password.UsuarioIdUsuario == userId.Value)
                    .OrderByDescending(password => password.FechaCreacion)
                    .Select(password => password.Contrasenia1)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    Logger.WarnFormat(
                        "Integrity issue: user {0} exists but has no password.",
                        userId.Value);

                    return OperationResult<AuthCredentialsDto>.Failure("Invalid credentials.");
                }

                var userProfile = context.Usuario
                    .AsNoTracking()
                    .Where(user => user.IdUsuario == userId.Value)
                    .Select(user => new { user.NombreUsuario, user.FotoPerfil })
                    .FirstOrDefault();

                if (userProfile == null)
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Profile data not found.");
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
            if (userId <= 0)
            {
                return OperationResult<IReadOnlyList<string>>.Failure("UserId must be positive.");
            }

            if (maxCount <= 0)
            {
                return OperationResult<IReadOnlyList<string>>.Failure("maxCount must be positive.");
            }

            try
            {
                using (var context = _contextFactory())
                {
                    ConfigureContext(context);

                    List<string> hashes = context.Contrasenia
                        .AsNoTracking()
                        .Where(password => password.UsuarioIdUsuario == userId)
                        .OrderByDescending(password => password.FechaCreacion)
                        .Take(maxCount)
                        .Select(password => password.Contrasenia1)
                        .ToList();

                    IReadOnlyList<string> readOnlyHashes = hashes;

                    return OperationResult<IReadOnlyList<string>>.Success(readOnlyHashes);
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading password history.", ex);
                return OperationResult<IReadOnlyList<string>>.Failure("Database error while loading password history.");
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading password history.", ex);
                return OperationResult<IReadOnlyList<string>>.Failure("Unexpected error while loading password history.");
            }
        }

        public OperationResult<bool> AddPasswordHash(int userId, string passwordHash)
        {
            if (userId <= 0)
            {
                return OperationResult<bool>.Failure("UserId must be positive.");
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return OperationResult<bool>.Failure("PasswordHash is required.");
            }

            try
            {
                using (var context = _contextFactory())
                {
                    ConfigureContext(context);

                    Cuenta activeAccount = GetActiveAccountForUser(context, userId);

                    if (activeAccount == null)
                    {
                        Logger.ErrorFormat(
                            "No active account found for user when changing password. UserId={0}",
                            userId);

                        return OperationResult<bool>.Failure("No active account found for user.");
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
                Logger.Error("SQL error while inserting new password.", ex);
                return OperationResult<bool>.Failure("Database error while updating password.");
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while inserting new password.", ex);
                return OperationResult<bool>.Failure("Unexpected error while updating password.");
            }
        }

        public OperationResult<string> GetEmailByUserId(int userId)
        {
            if (userId <= 0)
            {
                return OperationResult<string>.Failure("UserId must be positive.");
            }

            try
            {
                using (var context = _contextFactory())
                {
                    ConfigureContext(context);

                    Cuenta activeAccount = GetActiveAccountForUser(context, userId);

                    if (activeAccount == null)
                    {
                        Logger.WarnFormat(
                            "GetEmailByUserId: no active account found for user. UserId={0}",
                            userId);

                        return OperationResult<string>.Failure("Active account not found for user.");
                    }

                    string email = activeAccount.Correo ?? string.Empty;

                    return OperationResult<string>.Success(email);
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while loading email by user id.", ex);
                return OperationResult<string>.Failure("Database error while loading email.");
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while loading email by user id.", ex);
                return OperationResult<string>.Failure("Unexpected error while loading email.");
            }
        }

        private void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }

        private int? GetUserIdByEmail(SnakeAndLaddersDBEntities1 context, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.Correo == email)
                .ToList();

            if (accounts.Count == 0)
            {
                return null;
            }

            var activeAccounts = accounts
                .Where(account => IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == 0)
            {
                return null;
            }

            if (activeAccounts.Count > 1)
            {
                Logger.WarnFormat(
                    "Found {0} ACTIVE accounts with the same email {1}. Using the one with highest IdCuenta.",
                    activeAccounts.Count,
                    email);
            }

            var selectedAccount = activeAccounts
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

            var users = context.Usuario
                .AsNoTracking()
                .Where(user => user.NombreUsuario == username)
                .ToList();

            if (users.Count == 0)
            {
                return null;
            }

            var activeUsers = users
                .Where(user => IsActiveStatus(user.Estado))
                .ToList();

            if (activeUsers.Count == 0)
            {
                return null;
            }

            if (activeUsers.Count > 1)
            {
                Logger.WarnFormat(
                    "Found {0} ACTIVE users with the same NombreUsuario {1}. Using the one with highest IdUsuario.",
                    activeUsers.Count,
                    username);
            }

            var selectedUser = activeUsers
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
            var accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.UsuarioIdUsuario == userId)
                .ToList();

            if (accounts.Count == 0)
            {
                return null;
            }

            var activeAccounts = accounts
                .Where(account => IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == 0)
            {
                return null;
            }

            if (activeAccounts.Count > 1)
            {
                Logger.WarnFormat(
                    "Found {0} ACTIVE accounts for user {1}. Using the one with highest IdCuenta.",
                    activeAccounts.Count,
                    userId);
            }

            var selectedAccount = activeAccounts
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
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed.Substring(0, maxLength);
        }

        private static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(paramName + " is required.", paramName);
            }

            return value;
        }

        private static bool IsActiveStatus(byte[] status)
        {
            return status != null
                   && status.Length > 0
                   && status[0] == STATUS_ACTIVE;
        }
    }
}
