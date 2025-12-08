using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ReportRepository : IReportRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ReportRepository));

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int MIN_VALID_USER_ID = 1;

        private const string ERROR_DTO_REQUIRED = "ReportDto is required.";
        private const string ERROR_CRITERIA_REQUIRED = "ActiveReportSearchCriteriaDto is required.";
        private const string ERROR_REPORTED_USER_ID_POSITIVE = "ReportedUserId must be positive.";
        private const string ERROR_REPORTER_USER_ID_POSITIVE = "ReporterUserId must be positive.";

        private const string LOG_ERROR_INSERT_REPORT = "Error inserting report.";
        private const string LOG_ERROR_CHECK_ACTIVE_REPORT = "Error checking active report.";
        private const string LOG_ERROR_COUNT_ACTIVE_REPORTS = "Error counting active reports.";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public ReportRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void InsertReport(ReportDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), ERROR_DTO_REQUIRED);
            }

            if (dto.ReportedUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(dto.ReportedUserId), ERROR_REPORTED_USER_ID_POSITIVE);
            }

            if (dto.ReporterUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(dto.ReporterUserId), ERROR_REPORTER_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    Reporte entity = new Reporte
                    {
                        RazonReporte = dto.ReportReason,
                        IdUsuarioReportado = dto.ReportedUserId,
                        IdUsuarioQueReporta = dto.ReporterUserId
                    };

                    context.Reporte.Add(entity);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_INSERT_REPORT, ex);
                throw;
            }
        }

        public bool ReporterHasActiveReport(ActiveReportSearchCriteriaDto activeReportCriteria)
        {
            if (activeReportCriteria == null)
            {
                throw new ArgumentNullException(nameof(activeReportCriteria), ERROR_CRITERIA_REQUIRED);
            }

            if (activeReportCriteria.ReportedUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeReportCriteria.ReportedUserId),
                    ERROR_REPORTED_USER_ID_POSITIVE);
            }

            if (activeReportCriteria.ReporterUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeReportCriteria.ReporterUserId),
                    ERROR_REPORTER_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    IQueryable<Reporte> query = context.Reporte
                        .AsNoTracking()
                        .Where(report =>
                            report.IdUsuarioQueReporta == activeReportCriteria.ReporterUserId &&
                            report.IdUsuarioReportado == activeReportCriteria.ReportedUserId);

                    if (activeReportCriteria.LastSanctionDateUtc.HasValue)
                    {
                        DateTime startDate = activeReportCriteria.LastSanctionDateUtc.Value.Date;
                        query = query.Where(report => report.FechaReporte >= startDate);
                    }

                    bool hasActiveReport = query.Any();
                    return hasActiveReport;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_CHECK_ACTIVE_REPORT, ex);
                throw;
            }
        }

        public int CountActiveReportsAgainstUser(
            int reportedUserId,
            DateTime? lastSanctionDateUtc)
        {
            if (reportedUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(reportedUserId), ERROR_REPORTED_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ConfigureContext(context);

                    IQueryable<Reporte> query = context.Reporte
                        .AsNoTracking()
                        .Where(report => report.IdUsuarioReportado == reportedUserId);

                    if (lastSanctionDateUtc.HasValue)
                    {
                        DateTime startDate = lastSanctionDateUtc.Value.Date;
                        query = query.Where(report => report.FechaReporte >= startDate);
                    }

                    int activeReportsCount = query.Count();
                    return activeReportsCount;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_COUNT_ACTIVE_REPORTS, ex);
                throw;
            }
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
