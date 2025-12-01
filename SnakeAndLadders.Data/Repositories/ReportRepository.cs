using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ReportRepository : IReportRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReportRepository));

        private const int COMMAND_TIMEOUT_SECONDS = 30;

        public void InsertReport(ReportDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            try
            {
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    var entity = new Reporte
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
                Logger.Error("Error inserting report.", ex);
                throw;
            }
        }
        public bool ReporterHasActiveReport(ActiveReportSearchCriteriaDto activeReportCriteria)
        {
            if (activeReportCriteria == null)
            {
                throw new ArgumentNullException(nameof(activeReportCriteria));
            }

            try
            {
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

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

                    return query.Any();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking active report.", ex);
                throw;
            }
        }

        public int CountActiveReportsAgainstUser(
            int reportedUserId,
            DateTime? lastSanctionDateUtc)
        {
            try
            {
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    IQueryable<Reporte> query = context.Reporte
                        .AsNoTracking()
                        .Where(report => report.IdUsuarioReportado == reportedUserId);

                    if (lastSanctionDateUtc.HasValue)
                    {
                        DateTime startDate = lastSanctionDateUtc.Value.Date;
                        query = query.Where(report => report.FechaReporte >= startDate);
                    }

                    return query.Count();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error counting active reports.", ex);
                throw;
            }
        }
    }
}
