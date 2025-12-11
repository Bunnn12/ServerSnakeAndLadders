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
    public sealed class PasswordRepository : IPasswordRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PasswordRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public PasswordRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public OperationResult<IReadOnlyList<string>> GetLastPasswordHashes(int userId, int maxCount)
        {
            if (userId < AccountsRepositoryConstants.MIN_VALID_USER_ID)
            {
                return OperationResult<IReadOnlyList<string>>.Failure(
                    AccountsRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }

            if (maxCount < AccountsRepositoryConstants.MIN_PASSWORD_HISTORY_COUNT)
            {
                return OperationResult<IReadOnlyList<string>>.Failure(
                    AccountsRepositoryConstants.ERROR_MAX_COUNT_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    AccountsRepositoryHelper.ConfigureContext(context);

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
                _logger.Error(AccountsRepositoryConstants.LOG_SQL_ERROR_PASSWORD_HISTORY, ex);

                return OperationResult<IReadOnlyList<string>>.Failure(
                    AccountsRepositoryConstants.ERROR_DATABASE_LOADING_PASSWORD_HISTORY);
            }
            catch (Exception ex)
            {
                _logger.Error(AccountsRepositoryConstants.LOG_UNEXPECTED_ERROR_PASSWORD_HISTORY, ex);

                return OperationResult<IReadOnlyList<string>>.Failure(
                    AccountsRepositoryConstants.ERROR_UNEXPECTED_LOADING_PASSWORD_HISTORY);
            }
        }

        public OperationResult<bool> AddPasswordHash(int userId, string passwordHash)
        {
            if (userId < AccountsRepositoryConstants.MIN_VALID_USER_ID)
            {
                return OperationResult<bool>.Failure(
                    AccountsRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return OperationResult<bool>.Failure(
                    AccountsRepositoryConstants.ERROR_PASSWORD_HASH_REQUIRED);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    AccountsRepositoryHelper.ConfigureContext(context);

                    Cuenta activeAccount = AccountsRepositoryHelper.GetActiveAccountForUser(
                        context,
                        userId,
                        _logger);


                    if (activeAccount == null)
                    {
                        _logger.ErrorFormat(
                            AccountsRepositoryConstants.LOG_NO_ACTIVE_ACCOUNT_FOR_USER_CHANGING_PASSWORD,
                            userId);

                        return OperationResult<bool>.Failure(
                            AccountsRepositoryConstants.ERROR_ACTIVE_ACCOUNT_NOT_FOUND);
                    }

                    Contrasenia passwordEntity = new Contrasenia
                    {
                        UsuarioIdUsuario = userId,
                        CuentaIdCuenta = activeAccount.IdCuenta,
                        Contrasenia1 = passwordHash,
                        Estado = new[] { AccountsRepositoryConstants.STATUS_ACTIVE },
                        FechaCreacion = DateTime.UtcNow
                    };

                    context.Contrasenia.Add(passwordEntity);
                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(AccountsRepositoryConstants.LOG_SQL_ERROR_INSERT_PASSWORD, ex);

                return OperationResult<bool>.Failure(
                    AccountsRepositoryConstants.ERROR_DATABASE_UPDATING_PASSWORD);
            }
            catch (Exception ex)
            {
                _logger.Error(AccountsRepositoryConstants.LOG_UNEXPECTED_ERROR_INSERT_PASSWORD, ex);

                return OperationResult<bool>.Failure(
                    AccountsRepositoryConstants.ERROR_UNEXPECTED_UPDATING_PASSWORD);
            }
        }

    }
}
