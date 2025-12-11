using log4net;
using ServerSnakesAndLadders.Common;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Account
{
    public sealed class AccountEmailRepository : IAccountEmailRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountEmailRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountEmailRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public OperationResult<string> GetEmailByUserId(int userId)
        {
            if (userId < AccountsRepositoryConstants.MIN_VALID_USER_ID)
            {
                return OperationResult<string>.Failure(
                    AccountsRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    AccountsRepositoryHelper.ConfigureContext(context);

                    Cuenta activeAccount = AccountsRepositoryHelper.GetActiveAccountForUser(context,
                        userId,
                        _logger);

                    if (activeAccount == null)
                    {
                        _logger.WarnFormat(
                            AccountsRepositoryConstants.LOG_NO_ACTIVE_ACCOUNT_FOR_USER_GET_EMAIL,
                            userId);

                        return OperationResult<string>.Failure(
                            AccountsRepositoryConstants.ERROR_ACTIVE_ACCOUNT_NOT_FOUND);
                    }

                    string email = activeAccount.Correo ?? string.Empty;

                    return OperationResult<string>.Success(email);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(AccountsRepositoryConstants.LOG_SQL_ERROR_EMAIL_BY_USER_ID, ex);

                return OperationResult<string>.Failure(
                    AccountsRepositoryConstants.ERROR_DATABASE_LOADING_EMAIL);
            }
            catch (Exception ex)
            {
                _logger.Error(AccountsRepositoryConstants.LOG_UNEXPECTED_ERROR_EMAIL_BY_USER_ID, ex);

                return OperationResult<string>.Failure(
                    AccountsRepositoryConstants.ERROR_UNEXPECTED_LOADING_EMAIL);
            }
        }

        private Cuenta GetActiveAccountForUser(SnakeAndLaddersDBEntities1 context, int userId)
        {
            var accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.UsuarioIdUsuario == userId)
                .ToList();

            if (accounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            var activeAccounts = accounts
                .Where(account => AccountsRepositoryHelper.IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeAccounts.Count >= AccountsRepositoryConstants.MULTIPLE_ITEMS_MIN_COUNT)
            {
                _logger.WarnFormat(
                    AccountsRepositoryConstants.LOG_MULTIPLE_ACTIVE_ACCOUNTS_FOR_USER,
                    activeAccounts.Count,
                    userId);
            }

            return activeAccounts
                .OrderByDescending(account => account.IdCuenta)
                .First();
        }
    }
}
