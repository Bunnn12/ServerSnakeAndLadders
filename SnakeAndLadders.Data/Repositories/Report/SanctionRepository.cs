using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SanctionRepository : ISanctionRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SanctionRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public SanctionRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public void InsertSanction(SanctionDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(
                    nameof(dto),
                    SanctionRepositoryConstants.ERROR_DTO_REQUIRED);
            }

            SanctionRepositoryHelper.ValidateUserId(
                dto.UserId,
                nameof(dto.UserId),
                SanctionRepositoryConstants.ERROR_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    SanctionRepositoryHelper.ConfigureContext(dbContext);

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
            catch (DbUpdateException ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_INSERT_SANCTION, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_INSERT_SANCTION, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_INSERT_SANCTION, ex);
                throw;
            }
        }

        public SanctionDto GetLastSanctionForUser(int userId)
        {
            SanctionRepositoryHelper.ValidateUserId(
                userId,
                nameof(userId),
                SanctionRepositoryConstants.ERROR_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    SanctionRepositoryHelper.ConfigureContext(dbContext);

                    Sancion entity = dbContext.Sancion
                        .AsNoTracking()
                        .Where(sanction => sanction.UsuarioIdUsuario == userId)
                        .OrderByDescending(sanction => sanction.FechaSancion)
                        .FirstOrDefault();

                    if (entity == null)
                    {
                        return CreateEmptySanction(userId);
                    }

                    var dto = new SanctionDto
                    {
                        SanctionId = entity.IdSancion,
                        SanctionDateUtc = entity.FechaSancion,
                        SanctionType = entity.TipoSancion,
                        UserId = entity.UsuarioIdUsuario
                    };

                    return dto;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_GET_LAST_SANCTION, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_GET_LAST_SANCTION, ex);
                throw;
            }
        }

        public IList<SanctionDto> GetSanctionsHistory(int userId)
        {
            SanctionRepositoryHelper.ValidateUserId(
                userId,
                nameof(userId),
                SanctionRepositoryConstants.ERROR_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
                {
                    SanctionRepositoryHelper.ConfigureContext(dbContext);

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
            catch (InvalidOperationException ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_GET_SANCTIONS_HISTORY, ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(SanctionRepositoryConstants.LOG_ERROR_GET_SANCTIONS_HISTORY, ex);
                throw;
            }
        }

        private static SanctionDto CreateEmptySanction(int userId)
        {
            return new SanctionDto
            {
                SanctionId = SanctionRepositoryConstants.DEFAULT_SANCTION_ID,
                SanctionDateUtc = default(DateTime),
                SanctionType = SanctionRepositoryConstants.DEFAULT_SANCTION_TYPE,
                UserId = userId
            };
        }
    }
}
