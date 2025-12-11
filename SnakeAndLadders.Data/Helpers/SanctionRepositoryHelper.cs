using System;
using System.Data.Entity.Infrastructure;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class SanctionRepositoryHelper
    {
        internal static void ValidateUserId(
            int userId,
            string paramName,
            string errorMessage)
        {
            if (userId < SanctionRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(paramName, errorMessage);
            }
        }

        internal static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                SanctionRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }
    }
}
