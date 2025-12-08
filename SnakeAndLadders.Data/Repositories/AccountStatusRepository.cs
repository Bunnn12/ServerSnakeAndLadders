using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class AccountStatusRepository : IAccountStatusRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountStatusRepository));

        private const byte STATUS_ACTIVE_VALUE = 0x01;
        private const byte STATUS_INACTIVE_VALUE = 0x00;

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int MIN_VALID_USER_ID = 1;

        private const string DELETED_USERNAME_PREFIX = "deleted_";
        private const string DELETED_EMAIL_LOCAL_PART_PREFIX = "deleted+";
        private const string DELETED_EMAIL_DOMAIN = "invalid.local";

        private const string LOG_ERROR_UPDATING_ACTIVE_STATE =
            "Error updating active state for user. UserId={0}; IsActive={1}";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountStatusRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void SetUserAndAccountActiveState(int userId, bool isActive)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    byte[] dbStatus =
                    {
                        isActive
                            ? STATUS_ACTIVE_VALUE
                            : STATUS_INACTIVE_VALUE
                    };

                    Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == userId);
                    if (user != null)
                    {
                        user.Estado = dbStatus;

                        if (!isActive)
                        {
                            string deletedUserName = string.Format(
                                "{0}{1:D6}",
                                DELETED_USERNAME_PREFIX,
                                user.IdUsuario);

                            user.NombreUsuario = deletedUserName;
                        }
                    }

                    var accounts = context.Cuenta
                        .Where(account => account.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (Cuenta account in accounts)
                    {
                        account.Estado = dbStatus;

                        if (!isActive)
                        {
                            string deletedEmail = string.Format(
                                "{0}{1:D6}@{2}",
                                DELETED_EMAIL_LOCAL_PART_PREFIX,
                                userId,
                                DELETED_EMAIL_DOMAIN);

                            account.Correo = deletedEmail;
                        }
                    }

                    var passwords = context.Contrasenia
                        .Where(password => password.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (Contrasenia password in passwords)
                    {
                        password.Estado = dbStatus;
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(
                    LOG_ERROR_UPDATING_ACTIVE_STATE,
                    userId,
                    isActive);

                _logger.Error(message, ex);
                throw;
            }
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
