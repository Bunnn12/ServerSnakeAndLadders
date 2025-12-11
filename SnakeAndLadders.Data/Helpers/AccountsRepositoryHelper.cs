using log4net;
using SnakesAndLadders.Data.Constants;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class AccountsRepositoryHelper
    {
        public static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                AccountsRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }

        public static string NormalizeString(string value, int maxLength)
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

            return trimmed.Substring(
                AccountsRepositoryConstants.SUBSTRING_START_INDEX,
                maxLength);
        }

        public static string RequireParam(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string message = string.Format(
                    AccountsRepositoryConstants.ERROR_REQUIRED_TEMPLATE,
                    paramName);

                throw new ArgumentException(message, paramName);
            }

            return value;
        }

        public static bool IsActiveStatus(byte[] status)
        {
            return status != null
                   && status.Length >= AccountsRepositoryConstants.STATUS_MIN_LENGTH
                   && status[AccountsRepositoryConstants.STATUS_ACTIVE_INDEX] == AccountsRepositoryConstants.STATUS_ACTIVE;
        }

        public static Cuenta GetActiveAccountForUser(
           SnakeAndLaddersDBEntities1 context,
           int userId,
           ILog logger)
        {
            List<Cuenta> accounts = context.Cuenta
                .AsNoTracking()
                .Where(account => account.UsuarioIdUsuario == userId)
                .ToList();

            if (accounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            List<Cuenta> activeAccounts = accounts
                .Where(account => IsActiveStatus(account.Estado))
                .ToList();

            if (activeAccounts.Count == AccountsRepositoryConstants.EMPTY_COLLECTION_COUNT)
            {
                return null;
            }

            if (activeAccounts.Count >= AccountsRepositoryConstants.MULTIPLE_ITEMS_MIN_COUNT)
            {
                logger.WarnFormat(
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
