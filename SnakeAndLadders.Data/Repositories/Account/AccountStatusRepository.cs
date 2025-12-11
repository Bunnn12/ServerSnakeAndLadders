using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using System;
using System.Data.Entity.Infrastructure;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class AccountStatusRepository : IAccountStatusRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AccountStatusRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public AccountStatusRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void ActivateUserAndAccount(int userId)
        {
            AccountStatusChange change = CreateActivateChange(userId);
            UpdateUserAndAccountActiveState(change);
        }

        public void DeactivateUserAndAccount(int userId)
        {
            AccountStatusChange change = CreateDeactivateChange(userId);
            UpdateUserAndAccountActiveState(change);
        }

        private static AccountStatusChange CreateActivateChange(int userId)
        {
            ValidateUserId(userId);
            byte[] status = BuildStatusArray(true);
            return new AccountStatusChange(userId, true, status);
        }

        private static AccountStatusChange CreateDeactivateChange(int userId)
        {
            ValidateUserId(userId);
            byte[] status = BuildStatusArray(false);
            return new AccountStatusChange(userId, false, status);
        }

        private void UpdateUserAndAccountActiveState(AccountStatusChange change)
        {
            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    AccountStatusHelper.ConfigureContext(context);

                    AccountStatusHelper.UpdateUserStatus(context, change);
                    AccountStatusHelper.UpdateAccountsStatus(context, change);
                    AccountStatusHelper.UpdatePasswordsStatus(context, change);

                    context.SaveChanges();
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                LogActiveStateError(change, ex);
                throw;
            }
            catch (DbUpdateException ex)
            {
                LogActiveStateError(change, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogActiveStateError(change, ex);
                throw;
            }
            catch (Exception ex)
            {
                LogActiveStateError(change, ex);
                throw;
            }
        }
        private static void ValidateUserId(int userId)
        {
            if (userId < AccountStatusConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }
        }

        private static byte[] BuildStatusArray(bool isActive)
        {
            return new[]
            {
                isActive
                    ? AccountStatusConstants.STATUS_ACTIVE_VALUE
                    : AccountStatusConstants.STATUS_INACTIVE_VALUE
            };
        }

        private static void LogActiveStateError(AccountStatusChange change, Exception ex)
        {
            string message = string.Format(
                AccountStatusConstants.LOG_ERROR_UPDATING_ACTIVE_STATE,
                change.UserId,
                change.IsActive);

            _logger.Error(message, ex);
        }
    }
}
