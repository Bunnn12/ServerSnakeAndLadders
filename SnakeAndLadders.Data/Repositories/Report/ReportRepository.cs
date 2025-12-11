using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ReportRepository : IReportRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ReportRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public ReportRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void InsertReport(ReportDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), ReportRepositoryConstants.ERROR_DTO_REQUIRED);
            }

            ReportRepositoryHelper.ValidateUserId(
                dto.ReportedUserId,
                nameof(dto.ReportedUserId),
                ReportRepositoryConstants.ERROR_REPORTED_USER_ID_POSITIVE);

            ReportRepositoryHelper.ValidateUserId(
                dto.ReporterUserId,
                nameof(dto.ReporterUserId),
                ReportRepositoryConstants.ERROR_REPORTER_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ReportRepositoryHelper.ConfigureContext(context);

                    var entity = new Reporte
                    {
                        RazonReporte = dto.ReportReason,
                        IdUsuarioReportado = dto.ReportedUserId,
                        IdUsuarioQueReporta = dto.ReporterUserId,
                        FechaReporte = DateTime.UtcNow
                    };

                    context.Reporte.Add(entity);
                    context.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_INSERT_REPORT, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_INSERT_REPORT, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_INSERT_REPORT, ex);
                throw;
            }
        }

        public bool ReporterHasActiveReport(ActiveReportSearchCriteriaDto activeReportCriteria)
        {
            if (activeReportCriteria == null)
            {
                throw new ArgumentNullException(
                    nameof(activeReportCriteria),
                    ReportRepositoryConstants.ERROR_CRITERIA_REQUIRED);
            }

            ReportRepositoryHelper.ValidateUserId(
                activeReportCriteria.ReportedUserId,
                nameof(activeReportCriteria.ReportedUserId),
                ReportRepositoryConstants.ERROR_REPORTED_USER_ID_POSITIVE);

            ReportRepositoryHelper.ValidateUserId(
                activeReportCriteria.ReporterUserId,
                nameof(activeReportCriteria.ReporterUserId),
                ReportRepositoryConstants.ERROR_REPORTER_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ReportRepositoryHelper.ConfigureContext(context);

                    IQueryable<Reporte> query = context.Reporte
                        .AsNoTracking()
                        .Where(report =>
                            report.IdUsuarioQueReporta == activeReportCriteria.ReporterUserId &&
                            report.IdUsuarioReportado == activeReportCriteria.ReportedUserId);

                    query = ReportRepositoryHelper.ApplyStartDateFilter(
                        query,
                        activeReportCriteria.LastSanctionDateUtc);

                    bool hasActiveReport = query.Any();
                    return hasActiveReport;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_CHECK_ACTIVE_REPORT, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_CHECK_ACTIVE_REPORT, ex);
                throw;
            }
        }

        public int CountActiveReportsAgainstUser(
            int reportedUserId,
            DateTime? lastSanctionDateUtc)
        {
            ReportRepositoryHelper.ValidateUserId(
                reportedUserId,
                nameof(reportedUserId),
                ReportRepositoryConstants.ERROR_REPORTED_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    ReportRepositoryHelper.ConfigureContext(context);

                    IQueryable<Reporte> query = context.Reporte
                        .AsNoTracking()
                        .Where(report => report.IdUsuarioReportado == reportedUserId);

                    query = ReportRepositoryHelper.ApplyStartDateFilter(
                        query,
                        lastSanctionDateUtc);

                    int activeReportsCount = query.Count();
                    return activeReportsCount;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_COUNT_ACTIVE_REPORTS, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ReportRepositoryConstants.LOG_ERROR_COUNT_ACTIVE_REPORTS, ex);
                throw;
            }
        }
    }
}
