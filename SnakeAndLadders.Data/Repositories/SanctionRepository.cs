using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SanctionRepository : ISanctionRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SanctionRepository));

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int MIN_VALID_USER_ID = 1;
        private const int DEFAULT_SANCTION_ID = 0;

        private const string DEFAULT_SANCTION_TYPE = "";

        private const string ERROR_DTO_REQUIRED = "SanctionDto is required.";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

        private const string LOG_ERROR_INSERT_SANCTION = "Error inserting sanction.";
        private const string LOG_ERROR_GET_LAST_SANCTION = "Error getting last sanction.";
        private const string LOG_ERROR_GET_SANCTIONS_HISTORY = "Error getting sanctions history.";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public SanctionRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void InsertSanction(SanctionDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto), ERROR_DTO_REQUIRED);
            }

            if (dto.UserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(dto.UserId), ERROR_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    ConfigureContext(dbContext);

                    Sancion entity = new Sancion
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
                _logger.Error(LOG_ERROR_INSERT_SANCTION, ex);
                throw;
            }
        }

        public SanctionDto GetLastSanctionForUser(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    ConfigureContext(dbContext);

                    Sancion entity = dbContext.Sancion
                        .AsNoTracking()
                        .Where(sanction => sanction.UsuarioIdUsuario == userId)
                        .OrderByDescending(sanction => sanction.FechaSancion)
                        .FirstOrDefault();

                    if (entity == null)
                    {
                        return CreateEmptySanction(userId);
                    }

                    SanctionDto dto = new SanctionDto
                    {
                        SanctionId = entity.IdSancion,
                        SanctionDateUtc = entity.FechaSancion,
                        SanctionType = entity.TipoSancion,
                        UserId = entity.UsuarioIdUsuario
                    };

                    return dto;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_GET_LAST_SANCTION, ex);
                throw;
            }
        }

        public IList<SanctionDto> GetSanctionsHistory(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    ConfigureContext(dbContext);

                    List<SanctionDto> sanctions = dbContext.Sancion
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

                    return sanctions;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LOG_ERROR_GET_SANCTIONS_HISTORY, ex);
                throw;
            }
        }

        private static SanctionDto CreateEmptySanction(int userId)
        {
            SanctionDto dto = new SanctionDto
            {
                SanctionId = DEFAULT_SANCTION_ID,
                SanctionDateUtc = default(DateTime),
                SanctionType = DEFAULT_SANCTION_TYPE,
                UserId = userId
            };

            return dto;
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
