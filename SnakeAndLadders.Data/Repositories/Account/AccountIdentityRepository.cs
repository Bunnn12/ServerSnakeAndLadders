using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Data.Interfaces;
using SnakesAndLadders.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Account
{
    public sealed class AccountIdentityRepository : IAccountIdentityRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountIdentityRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountIdentityRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public bool IsEmailRegistered(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string normalizedEmail = AccountsRepositoryHelper.NormalizeString(
                email,
                AccountsRepositoryConstants.EMAIL_MAX_LENGTH);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                AccountsRepositoryHelper.ConfigureContext(context);

                List<Cuenta> accounts = context.Cuenta
                    .AsNoTracking()
                    .Where(account => account.Correo == normalizedEmail)
                    .ToList();

                return accounts.Any(account => AccountsRepositoryHelper.IsActiveStatus(account.Estado));
            }
        }

        public bool IsUserNameTaken(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            string normalizedUserName = AccountsRepositoryHelper.NormalizeString(
                userName,
                AccountsRepositoryConstants.USERNAME_MAX_LENGTH);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                AccountsRepositoryHelper.ConfigureContext(context);

                List<Usuario> users = context.Usuario
                    .AsNoTracking()
                    .Where(user => user.NombreUsuario == normalizedUserName)
                    .ToList();

                return users.Any(user => AccountsRepositoryHelper.IsActiveStatus(user.Estado));
            }
        }

        public OperationResult<AuthCredentialsDto> GetAuthByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return OperationResult<AuthCredentialsDto>.Failure(
                    AccountsRepositoryConstants.ERROR_IDENTIFIER_REQUIRED);
            }

            string trimmedIdentifier = identifier.Trim();

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                AccountsRepositoryHelper.ConfigureContext(context);

                int? userId = FindUserIdByIdentifier(context, trimmedIdentifier);

                if (!userId.HasValue || !IsActiveUser(context, userId.Value))
                {
                    return OperationResult<AuthCredentialsDto>.Failure(
                        AccountsRepositoryConstants.ERROR_INVALID_USERNAME_OR_PASSWORD);
                }

                string passwordHash = LoadLatestPasswordHash(context, userId.Value);
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    _logger.WarnFormat(
                        AccountsRepositoryConstants.LOG_INTEGRITY_USER_WITHOUT_PASSWORD,
                        userId.Value);

                    return OperationResult<AuthCredentialsDto>.Failure(
                        AccountsRepositoryConstants.ERROR_INVALID_CREDENTIALS);
                }

                AuthCredentialsDto credentialsDto = BuildAuthCredentials(context, userId.Value, passwordHash);
                if (credentialsDto == null)
                {
                    return OperationResult<AuthCredentialsDto>.Failure(
                        AccountsRepositoryConstants.ERROR_PROFILE_DATA_NOT_FOUND);
                }

                return OperationResult<AuthCredentialsDto>.Success(credentialsDto);
            }
        }

        private int? FindUserIdByIdentifier(SnakeAndLaddersDBEntities1 context, string identifier)
        {
            int? userIdByEmail = GetUserIdByEmail(context, identifier);
            if (userIdByEmail.HasValue)
            {
                return userIdByEmail;
            }

            return GetUserIdByUsername(context, identifier);
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

            if (accounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            Cuenta selectedAccount = SelectSingleActiveAccount(
                accounts,
                AccountsRepositoryConstants.LOG_MULTIPLE_ACTIVE_ACCOUNTS_EMAIL,
                email);

            return selectedAccount?.UsuarioIdUsuario;
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

            if (users.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            Usuario selectedUser = SelectSingleActiveUser(
                users,
                AccountsRepositoryConstants.LOG_MULTIPLE_ACTIVE_USERS_USERNAME,
                username);

            return selectedUser?.IdUsuario;
        }

        private bool IsActiveUser(SnakeAndLaddersDBEntities1 context, int userId)
        {
            Usuario userEntity = context.Usuario
                .AsNoTracking()
                .SingleOrDefault(user => user.IdUsuario == userId);

            if (userEntity == null)
            {
                return false;
            }

            return AccountsRepositoryHelper.IsActiveStatus(userEntity.Estado);
        }

        private string LoadLatestPasswordHash(SnakeAndLaddersDBEntities1 context, int userId)
        {
            return context.Contrasenia
                .AsNoTracking()
                .Where(password => password.UsuarioIdUsuario == userId)
                .OrderByDescending(password => password.FechaCreacion)
                .Select(password => password.Contrasenia1)
                .FirstOrDefault();
        }

        private AuthCredentialsDto BuildAuthCredentials(
            SnakeAndLaddersDBEntities1 context,
            int userId,
            string passwordHash)
        {
            var userProfile = context.Usuario
                .AsNoTracking()
                .Where(user => user.IdUsuario == userId)
                .Select(user => new { user.NombreUsuario, user.FotoPerfil })
                .FirstOrDefault();

            if (userProfile == null)
            {
                return null;
            }

            return new AuthCredentialsDto
            {
                UserId = userId,
                PasswordHash = passwordHash,
                DisplayName = userProfile.NombreUsuario,
                ProfilePhotoId = AvatarIdHelper.MapFromDb(userProfile.FotoPerfil)
            };
        }

        private Cuenta SelectSingleActiveAccount(
            IEnumerable<Cuenta> accounts,
            string multipleItemsLogTemplate,
            object logParam)
        {
            List<Cuenta> activeAccounts = accounts
                .Where(account => AccountsRepositoryHelper.IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeAccounts.Count >= AccountsRepositoryConstants.MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    multipleItemsLogTemplate,
                    activeAccounts.Count,
                    logParam);
            }

            return activeAccounts
                .OrderByDescending(account => account.IdCuenta)
                .First();
        }

        private Usuario SelectSingleActiveUser(
            IEnumerable<Usuario> users,
            string multipleItemsLogTemplate,
            object logParam)
        {
            List<Usuario> activeUsers = users
                .Where(user => AccountsRepositoryHelper.IsActiveStatus(user.Estado))
                .ToList();

            if (activeUsers.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeUsers.Count >= AccountsRepositoryConstants.MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    multipleItemsLogTemplate,
                    activeUsers.Count,
                    logParam);
            }

            return activeUsers
                .OrderByDescending(user => user.IdUsuario)
                .First();
        }
    }
}
