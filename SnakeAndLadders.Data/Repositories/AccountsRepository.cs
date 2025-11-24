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
        // --- CONSTANTES ---
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

        // --- CONSTRUCTOR ---
        public AccountsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        // --- MÉTODOS BOOLEANOS (Se quedan igual) ---
        public bool IsEmailRegistered(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string normalizedEmail = NormalizeString(email, EmailMaxLength);

            using (var context = _contextFactory())
            {
                ConfigureContext(context);
                return context.Cuenta.AsNoTracking().Any(c => c.Correo == normalizedEmail);
            }
        }

        public bool IsUserNameTaken(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;
            string normalizedUserName = NormalizeString(userName, UsernameMaxLength);

            using (var context = _contextFactory())
            {
                ConfigureContext(context);
                return context.Usuario.AsNoTracking().Any(u => u.NombreUsuario == normalizedUserName);
            }
        }

        // --- MÉTODOS CON OPERATION RESULT ---

        // CAMBIO: Ahora retorna OperationResult<int>
        public OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto request)
        {
            if (request == null)
                return OperationResult<int>.Failure("La solicitud no puede ser nula.");

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

                        // ÉXITO
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

        // CAMBIO: Ahora retorna OperationResult<AuthCredentialsDto>
        public OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return OperationResult<AuthCredentialsDto>.Failure("El identificador es requerido.");

            string trimmedIdentifier = identifier.Trim();

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                var userId = GetUserIdByEmail(context, trimmedIdentifier)
                             ?? GetUserIdByUsername(context, trimmedIdentifier);

                if (!userId.HasValue)
                {
                    return OperationResult<AuthCredentialsDto>.Failure("Usuario o contraseña incorrectos.");
                }

                var passwordHash = context.Contrasenia
                    .AsNoTracking()
                    .Where(p => p.UsuarioIdUsuario == userId.Value)
                    .OrderByDescending(p => p.FechaCreacion)
                    .Select(p => p.Contrasenia1)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(passwordHash))
                {
                    Logger.Warn($"Integridad: El usuario {userId.Value} existe pero no tiene contraseña.");
                    return OperationResult<AuthCredentialsDto>.Failure("Credenciales inválidas.");
                }

                var userProfile = context.Usuario
                    .AsNoTracking()
                    .Where(u => u.IdUsuario == userId.Value)
                    .Select(u => new { u.NombreUsuario, u.FotoPerfil })
                    .FirstOrDefault();

                if (userProfile == null)
                    return OperationResult<AuthCredentialsDto>.Failure("No se encontraron datos del perfil.");

                var credentialsDto = new AuthCredentialsDto
                {
                    UserId = userId.Value,
                    PasswordHash = passwordHash,
                    DisplayName = userProfile.NombreUsuario,
                    ProfilePhotoId = AvatarIdHelper.MapFromDb(userProfile.FotoPerfil)
                };

                // ÉXITO
                return OperationResult<AuthCredentialsDto>.Success(credentialsDto);
            }
        }

        // --- HELPERS (Privados) ---

        private void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = CommandTimeoutSeconds;
        }

        private int? GetUserIdByEmail(SnakeAndLaddersDBEntities1 context, string email)
        {
            return context.Cuenta
                .AsNoTracking()
                .Where(c => c.Correo == email)
                .Select(c => (int?)c.UsuarioIdUsuario)
                .FirstOrDefault();
        }

        private int? GetUserIdByUsername(SnakeAndLaddersDBEntities1 context, string username)
        {
            return context.Usuario
                .AsNoTracking()
                .Where(u => u.NombreUsuario == username)
                .Select(u => (int?)u.IdUsuario)
                .FirstOrDefault();
        }

        private (Usuario User, Cuenta Account, Contrasenia Password) PrepareUserData(CreateAccountRequestDto request)
        {
            string userName = RequireParam(NormalizeString(request.Username, UsernameMaxLength), nameof(request.Username));
            string email = RequireParam(NormalizeString(request.Email, EmailMaxLength), nameof(request.Email));
            string passwordHash = RequireParam(NormalizeString(request.PasswordHash, PasswordHashMaxLength), nameof(request.PasswordHash));

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
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} es requerido.", paramName);
            return value;
        }
    }
}