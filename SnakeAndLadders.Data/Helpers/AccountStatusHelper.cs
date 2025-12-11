using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Constants;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class AccountStatusHelper
    {
        public static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                AccountStatusConstants.COMMAND_TIMEOUT_SECONDS;
        }

        public static void UpdateUserStatus(
            SnakeAndLaddersDBEntities1 context,
            AccountStatusChange change)
        {
            Usuario user = context.Usuario
                .SingleOrDefault(u => u.IdUsuario == change.UserId);

            if (user == null)
            {
                return;
            }

            user.Estado = change.Status;

            if (!change.IsActive)
            {
                user.NombreUsuario = BuildDeletedUserName(change);
            }
        }

        public static void UpdateAccountsStatus(
            SnakeAndLaddersDBEntities1 context,
            AccountStatusChange change)
        {
            var accounts = context.Cuenta
                .Where(account => account.UsuarioIdUsuario == change.UserId)
                .ToList();

            foreach (Cuenta account in accounts)
            {
                account.Estado = change.Status;

                if (!change.IsActive)
                {
                    account.Correo = BuildDeletedEmail(change);
                }
            }
        }

        public static void UpdatePasswordsStatus(
            SnakeAndLaddersDBEntities1 context,
            AccountStatusChange change)
        {
            var passwords = context.Contrasenia
                .Where(password => password.UsuarioIdUsuario == change.UserId)
                .ToList();

            foreach (Contrasenia password in passwords)
            {
                password.Estado = change.Status;
            }
        }

        private static string BuildDeletedUserName(AccountStatusChange change)
        {
            return string.Format(
                "{0}{1:D" + AccountStatusConstants.DELETED_USER_ID_PAD_LENGTH + "}",
                AccountStatusConstants.DELETED_USERNAME_PREFIX,
                change.UserId);
        }

        private static string BuildDeletedEmail(AccountStatusChange change)
        {
            return string.Format(
                "{0}{1:D" + AccountStatusConstants.DELETED_USER_ID_PAD_LENGTH + "}@{2}",
                AccountStatusConstants.DELETED_EMAIL_LOCAL_PART_PREFIX,
                change.UserId,
                AccountStatusConstants.DELETED_EMAIL_DOMAIN);
        }
    }
}
