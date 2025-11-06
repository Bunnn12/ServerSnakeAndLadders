using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ReportRepository : IReportRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReportRepository));

        public ReportRepository()
        {
        }

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
                    IQueryable<Reporte> query = context.Reporte.AsNoTracking()
                        .Where(r =>
                            r.IdUsuarioQueReporta == activeReportCriteria.ReporterUserId &&
                            r.IdUsuarioReportado == activeReportCriteria.ReportedUserId);

                    if (activeReportCriteria.LastSanctionDateUtc.HasValue)
                    {
                        DateTime startDate = activeReportCriteria.LastSanctionDateUtc.Value.Date;
                        query = query.Where(r => r.FechaReporte >= startDate);
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
                    IQueryable<Reporte> query = context.Reporte.AsNoTracking()
                        .Where(r => r.IdUsuarioReportado == reportedUserId);

                    if (lastSanctionDateUtc.HasValue)
                    {
                        DateTime startDate = lastSanctionDateUtc.Value.Date;
                        query = query.Where(r => r.FechaReporte >= startDate);
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