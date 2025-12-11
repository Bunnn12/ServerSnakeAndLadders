using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using log4net;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class RepositoryExceptionHandler
    {
        public static void Handle(Exception ex, ILog logger)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (ex is DbEntityValidationException)
            {
                logger.Error("Validation error in repository operation.", ex);
                return;
            }

            if (ex is DbUpdateException)
            {
                logger.Error("Update error in repository operation.", ex);
                return;
            }

            if (ex is SqlException)
            {
                logger.Error("SQL error in repository operation.", ex);
                return;
            }

            if (ex is EntityException)
            {
                logger.Error("Entity error in repository operation.", ex);
                return;
            }

            logger.Error("Unexpected error in repository operation.", ex);
        }
    }
}
