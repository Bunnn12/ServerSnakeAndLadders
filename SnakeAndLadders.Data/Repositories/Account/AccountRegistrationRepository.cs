using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Account
{
    public sealed class AccountRegistrationRepository : IAccountRegistrationRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountRegistrationRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountRegistrationRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public OperationResult<int> CreateUserWithAccountAndPassword(CreateAccountRequestDto request)
        {
            if (request == null)
            {
                return OperationResult<int>.Failure(
                    AccountsRepositoryConstants.ERROR_REQUEST_NULL);
            }

            try
            {
                (Usuario User, Cuenta Account, Contrasenia Password) userData =
                    PrepareUserData(request);

                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int userId = SaveUserGraph(context, userData);

                        transaction.Commit();

                        return OperationResult<int>.Success(userId);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        _logger.Error(AccountsRepositoryConstants.LOG_SQL_ERROR_CREATE_USER, ex);

                        return OperationResult<int>.Failure(
                            AccountsRepositoryConstants.ERROR_DATABASE_REGISTERING_USER);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.Error(AccountsRepositoryConstants.LOG_UNEXPECTED_ERROR_CREATE_USER, ex);

                        return OperationResult<int>.Failure(
                            AccountsRepositoryConstants.ERROR_UNEXPECTED_REGISTERING_USER);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                return OperationResult<int>.Failure(ex.Message);
            }
        }

        private (Usuario User, Cuenta Account, Contrasenia Password) PrepareUserData(CreateAccountRequestDto request)
        {
            string userName = AccountsRepositoryHelper.RequireParam(
                AccountsRepositoryHelper.NormalizeString(
                    request.Username,
                    AccountsRepositoryConstants.USERNAME_MAX_LENGTH),
                nameof(request.Username));

            string email = AccountsRepositoryHelper.RequireParam(
                AccountsRepositoryHelper.NormalizeString(
                    request.Email,
                    AccountsRepositoryConstants.EMAIL_MAX_LENGTH),
                nameof(request.Email));

            string passwordHash = AccountsRepositoryHelper.RequireParam(
                AccountsRepositoryHelper.NormalizeString(
                    request.PasswordHash,
                    AccountsRepositoryConstants.PASSWORD_HASH_MAX_LENGTH),
                nameof(request.PasswordHash));

            Usuario user = new Usuario
            {
                NombreUsuario = userName,
                Nombre = AccountsRepositoryHelper.NormalizeString(
                    request.FirstName,
                    AccountsRepositoryConstants.USERNAME_MAX_LENGTH),
                Apellidos = AccountsRepositoryHelper.NormalizeString(
                    request.LastName,
                    AccountsRepositoryConstants.USERNAME_MAX_LENGTH),
                DescripcionPerfil = AccountsRepositoryHelper.NormalizeString(
                    request.ProfileDescription,
                    AccountsRepositoryConstants.PROFILE_DESC_MAX_LENGTH),
                FotoPerfil = AccountsRepositoryHelper.NormalizeString(
                    request.ProfilePhotoId,
                    AccountsRepositoryConstants.PROFILE_PHOTO_ID_MAX_LENGTH),
                Monedas = AccountsRepositoryConstants.INITIAL_COINS,
                Estado = new[] { AccountsRepositoryConstants.STATUS_ACTIVE }
            };

            Cuenta account = new Cuenta
            {
                Correo = email,
                Estado = new[] { AccountsRepositoryConstants.STATUS_ACTIVE }
            };

            Contrasenia password = new Contrasenia
            {
                Contrasenia1 = passwordHash,
                Estado = new[] { AccountsRepositoryConstants.STATUS_ACTIVE },
                FechaCreacion = DateTime.UtcNow
            };

            return (user, account, password);
        }

        private int SaveUserGraph(
            SnakeAndLaddersDBEntities1 context,
            (Usuario User, Cuenta Account, Contrasenia Password) userData)
        {
            AccountsRepositoryHelper.ConfigureContext(context);

            context.Usuario.Add(userData.User);
            context.SaveChanges();

            userData.Account.UsuarioIdUsuario = userData.User.IdUsuario;
            context.Cuenta.Add(userData.Account);
            context.SaveChanges();

            userData.Password.UsuarioIdUsuario = userData.User.IdUsuario;
            userData.Password.Cuenta = userData.Account;
            context.Contrasenia.Add(userData.Password);
            context.SaveChanges();

            return userData.User.IdUsuario;
        }
    }
}
