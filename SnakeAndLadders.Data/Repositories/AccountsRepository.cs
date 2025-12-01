using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace ServerSnakesAndLadders
{
    /// <summary>
    /// Repository responsible for user, account and authentication persistence operations.
    /// </summary>
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

                return context.Cuenta
                    .AsNoTracking()
                    .Where(c => c.Correo == normalizedEmail)
                    .ToList()
                    .Any(c => IsActiveStatus(c.Estado));
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

                return context.Usuario
                    .AsNoTracking()
                    .Where(u => u.NombreUsuario == normalizedUserName)
                    .ToList()
                    .Any(u => IsActiveStatus(u.Estado));
            }
        }


        public OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto request)
        {
            if (request == null)
            {
                return OperationResult<int>.Failure("La solicitud no puede ser nula.");
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
                        Logger.Error("Error SQL al crear usuario.", ex);
                        return OperationResult<int>.Failure("Error de base de datos al registrar usuario.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Error inesperado al crear usuario.", ex);
                        return OperationResult<int>.Failure("Ocurrió un error inesperado.");
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
                return OperationResult<AuthCredentialsDto>.Failure("El identificador es requerido.");
            }

            string trimmedIdentifier = identifier.Trim();

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                int? userId = GetUserIdByEmail(context, trimmedIdentifier)
                              ?? GetUserIdByUsername(context, trimmedIdentifier);

                if (!userId.HasValue)
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Usuario o contraseña incorrectos.");
                }

                var userEntity = context.Usuario
                    .AsNoTracking()
                    .SingleOrDefault(u => u.IdUsuario == userId.Value);

                if (userEntity == null || !IsActiveStatus(userEntity.Estado))
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Usuario o contraseña incorrectos.");
                }

                string passwordHash = context.Contrasenia
                    .AsNoTracking()
                    .Where(p => p.UsuarioIdUsuario == userId.Value)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => p.Contrasenia1)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(passwordHash))
                {
                    Logger.WarnFormat(
                        "Integridad: El usuario {0} existe pero no tiene contraseña.",
                        userId.Value);

                    return OperationResult<AuthCredentialsDto>.Failure("Credenciales inválidas.");
                }

                var userProfile = context.Usuario
                    .AsNoTracking()
                    .Where(u => u.IdUsuario == userId.Value)
                    .Select(u => new { u.NombreUsuario, u.FotoPerfil })
                    .FirstOrDefault();

                if (userProfile == null)
                {
                    return OperationResult<AuthCredentialsDto>.Failure("No se encontraron datos del perfil.");
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
                .Where(c => c.Correo == email)
                .ToList();

            if (accounts.Count == 0)
            {
                return null;
            }

            var activeAccounts = accounts
                .Where(a => IsActiveStatus(a.Estado))
                .ToList();

            if (activeAccounts.Count == 0)
            {
                return null;
            }

            if (activeAccounts.Count > 1)
            {
                Logger.WarnFormat(
                    "Se encontraron {0} cuentas ACTIVAS con el mismo correo {1}. " +
                    "Se usará la de mayor IdCuenta.",
                    activeAccounts.Count,
                    email);
            }

            var selected = activeAccounts
                .OrderByDescending(a => a.IdCuenta) 
                .First();

            return selected.UsuarioIdUsuario;
        }

        private int? GetUserIdByUsername(SnakeAndLaddersDBEntities1 context, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var users = context.Usuario
                .AsNoTracking()
                .Where(u => u.NombreUsuario == username)
                .ToList();

            if (users.Count == 0)
            {
                return null;
            }

            var activeUsers = users
                .Where(u => IsActiveStatus(u.Estado))
                .ToList();

            if (activeUsers.Count == 0)
            {
                return null;
            }

            if (activeUsers.Count > 1)
            {
                Logger.WarnFormat(
                    "Se encontraron {0} usuarios ACTIVOS con el mismo NombreUsuario {1}. " +
                    "Se usará el de mayor IdUsuario.",
                    activeUsers.Count,
                    username);
            }

            var selected = activeUsers
                .OrderByDescending(u => u.IdUsuario)
                .First();

            return selected.IdUsuario;
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
                        .Where(p => p.UsuarioIdUsuario == userId)
                        .OrderByDescending(p => p.FechaCreacion)
                        .Take(maxCount)
                        .Select(p => p.Contrasenia1)
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

                    var account = context.Cuenta
                        .AsNoTracking()
                        .Where(c => c.UsuarioIdUsuario == userId)
                        .ToList()
                        .Where(c => IsActiveStatus(c.Estado))
                        .OrderByDescending(c => c.IdCuenta)
                        .FirstOrDefault();

                    if (account == null)
                    {
                        Logger.ErrorFormat(
                            "No active account found for user when changing password. UserId={0}",
                            userId);

                        return OperationResult<bool>.Failure("No active account found for user.");
                    }

                    var passwordEntity = new Contrasenia
                    {
                        UsuarioIdUsuario = userId,
                        CuentaIdCuenta = account.IdCuenta,
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
        private Cuenta GetActiveAccountForUser(SnakeAndLaddersDBEntities1 context, int userId)
        {
            var accounts = context.Cuenta
                .AsNoTracking()
                .Where(c => c.UsuarioIdUsuario == userId)
                .ToList();

            if (accounts.Count == 0)
            {
                return null;
            }

            var activeAccounts = accounts
                .Where(a => IsActiveStatus(a.Estado))
                .ToList();

            if (activeAccounts.Count == 0)
            {
                return null;
            }

            if (activeAccounts.Count > 1)
            {
                Logger.WarnFormat(
                    "Se encontraron {0} cuentas ACTIVAS para el usuario {1}. Se usará la de mayor IdCuenta.",
                    activeAccounts.Count,
                    userId);
            }

            var selected = activeAccounts
                .OrderByDescending(a => a.IdCuenta)
                .First();

            return selected;
        }


        public string GetEmailByUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                var accounts = context.Cuenta
                    .AsNoTracking()
                    .Where(c => c.UsuarioIdUsuario == userId)
                    .ToList();

                if (accounts.Count == 0)
                {
                    return null;
                }

                var activeAccounts = accounts
                    .Where(a => IsActiveStatus(a.Estado))
                    .ToList();

                if (activeAccounts.Count == 0)
                {
                    return null;
                }

                var selected = activeAccounts
                    .OrderByDescending(a => a.IdCuenta)
                    .First();

                return selected.Correo;
            }
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
                throw new ArgumentException(paramName + " es requerido.", paramName);
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
