using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Server.Helpers;
using System;
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
        private const int EmailMaxLength = 200;
        private const int UsernameMaxLength = 90;
        private const int ProfileDescMaxLength = 510;
        private const int PasswordHashMaxLength = 510;
        private const int ProfilePhotoIdMaxLength = 5;
        private const int CommandTimeoutSeconds = 30;
        private const int InitialCoins = 0;
        private const byte StatusActive = 1;

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

            string normalizedEmail = NormalizeString(email, EmailMaxLength);

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

            string normalizedUserName = NormalizeString(userName, UsernameMaxLength);

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
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = CommandTimeoutSeconds;
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
                NormalizeString(request.Username, UsernameMaxLength),
                nameof(request.Username));

            string email = RequireParam(
                NormalizeString(request.Email, EmailMaxLength),
                nameof(request.Email));

            string passwordHash = RequireParam(
                NormalizeString(request.PasswordHash, PasswordHashMaxLength),
                nameof(request.PasswordHash));

            var user = new Usuario
            {
                NombreUsuario = userName,
                Nombre = NormalizeString(request.FirstName, UsernameMaxLength),
                Apellidos = NormalizeString(request.LastName, UsernameMaxLength),
                DescripcionPerfil = NormalizeString(request.ProfileDescription, ProfileDescMaxLength),
                FotoPerfil = NormalizeString(request.ProfilePhotoId, ProfilePhotoIdMaxLength),
                Monedas = InitialCoins,
                Estado = new[] { StatusActive }
            };

            var account = new Cuenta
            {
                Correo = email,
                Estado = new[] { StatusActive }
            };

            var password = new Contrasenia
            {
                Contrasenia1 = passwordHash,
                Estado = new[] { StatusActive },
                FechaCreacion = DateTime.UtcNow
            };

            return (user, account, password);
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
                   && status[0] == StatusActive;
        }
    }
}
