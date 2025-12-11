using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class ReportRepositoryHelper
    {
        internal static void ValidateUserId(
            int userId,
            string paramName,
            string errorMessage)
        {
            if (userId < ReportRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(paramName, errorMessage);
            }
        }

        internal static IQueryable<Reporte> ApplyStartDateFilter(
            IQueryable<Reporte> query,
            DateTime? lastSanctionDateUtc)
        {
            if (!lastSanctionDateUtc.HasValue)
            {
                return query;
            }

            DateTime startDate = lastSanctionDateUtc.Value.Date;
            return query.Where(report => report.FechaReporte >= startDate);
        }

        internal static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                ReportRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }
    }
}
