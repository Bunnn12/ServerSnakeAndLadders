using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    
    public sealed class SanctionRepository : ISanctionRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SanctionRepository));

        private const int COMMAND_TIMEOUT_SECONDS = 30;

        public SanctionRepository()
        {
        }

        public void InsertSanction(SanctionDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            try
            {
                using (var dbContext = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    var entity = new Sancion
                    {
                        FechaSancion = dto.SanctionDateUtc,
                        TipoSancion = dto.SanctionType,
                        UsuarioIdUsuario = dto.UserId
                    };

                    dbContext.Sancion.Add(entity);
                    dbContext.SaveChanges();

                    dto.SanctionId = entity.IdSancion;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error inserting sanction.", ex);
                throw;
            }
        }
        public SanctionDto GetLastSanctionForUser(int userId)
        {
            try
            {
                using (var dbContext = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    var entity = dbContext.Sancion
                        .AsNoTracking()
                        .Where(sanction => sanction.UsuarioIdUsuario == userId)
                        .OrderByDescending(sanction => sanction.FechaSancion)
                        .FirstOrDefault();

                    if (entity == null)
                    {
                        return null;
                    }

                    return new SanctionDto
                    {
                        SanctionId = entity.IdSancion,
                        SanctionDateUtc = entity.FechaSancion,
                        SanctionType = entity.TipoSancion,
                        UserId = entity.UsuarioIdUsuario
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting last sanction.", ex);
                throw;
            }
        }

        public IList<SanctionDto> GetSanctionsHistory(int userId)
        {
            try
            {
                using (var dbContext = new SnakeAndLaddersDBEntities1())
                {
                    ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                    return dbContext.Sancion
                        .AsNoTracking()
                        .Where(sanction => sanction.UsuarioIdUsuario == userId)
                        .OrderByDescending(sanction => sanction.FechaSancion)
                        .Select(sanction => new SanctionDto
                        {
                            SanctionId = sanction.IdSancion,
                            SanctionDateUtc = sanction.FechaSancion,
                            SanctionType = sanction.TipoSancion,
                            UserId = sanction.UsuarioIdUsuario
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting sanctions history.", ex);
                throw;
            }
        }
    }
}
