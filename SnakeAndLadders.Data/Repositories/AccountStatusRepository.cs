using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    /// <summary>
    /// Handles activation and deactivation of user-related records
    /// (user, accounts and passwords) in the database.
    /// </summary>
    public sealed class AccountStatusRepository : IAccountStatusRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountStatusRepository));

        private const byte STATUS_ACTIVE_VALUE = 0x01;
        private const byte STATUS_INACTIVE_VALUE = 0x00;

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const string DELETED_USERNAME_PREFIX = "deleted_";
        private const string DELETED_EMAIL_DOMAIN = "invalid.local";

        public void SetUserAndAccountActiveState(int userId, bool isActive)
        {
            if (userId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            try
            {
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    byte[] dbStatus = new[] { isActive ? STATUS_ACTIVE_VALUE : STATUS_INACTIVE_VALUE };

                    var user = context.Usuario.SingleOrDefault(u => u.IdUsuario == userId);
                    if (user != null)
                    {
                        user.Estado = dbStatus;

                        if (!isActive)
                        {
                            string deletedUserName = $"deleted_{user.IdUsuario:D6}";
                            user.NombreUsuario = deletedUserName;
                        }
                    }

                    var accounts = context.Cuenta
                        .Where(account => account.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (var account in accounts)
                    {
                        account.Estado = dbStatus;

                        if (!isActive)
                        {
                            string deletedEmail = $"deleted+{userId:D6}@invalid.local";
                            account.Correo = deletedEmail;
                        }
                    }

                    var passwords = context.Contrasenia
                        .Where(password => password.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (var password in passwords)
                    {
                        password.Estado = dbStatus;
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(
                    "Error updating active state for user. UserId={0}; IsActive={1}",
                    userId,
                    isActive);

                Logger.Error(message, ex);
                throw;
            }
        }
    }
}
