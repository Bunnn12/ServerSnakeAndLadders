using System;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class AccountStatusRepository : IAccountStatusRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountStatusRepository));

        private const byte STATUS_ACTIVE_VALUE = 0x01;
        private const byte STATUS_INACTIVE_VALUE = 0x00;

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
                    byte[] dbStatus = new[] { isActive ? STATUS_ACTIVE_VALUE : STATUS_INACTIVE_VALUE };

                    var user = context.Usuario.SingleOrDefault(u => u.IdUsuario == userId);
                    if (user != null)
                    {
                        user.Estado = dbStatus;
                    }

                    var accounts = context.Cuenta
                        .Where(c => c.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (var account in accounts)
                    {
                        account.Estado = dbStatus;
                    }

                    var passwords = context.Contrasenia
                        .Where(p => p.UsuarioIdUsuario == userId)
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
                Logger.Error(
                    $"Error updating active state for user {userId}. IsActive={isActive}",
                    ex);

                throw;
            }
        }
    }
}
