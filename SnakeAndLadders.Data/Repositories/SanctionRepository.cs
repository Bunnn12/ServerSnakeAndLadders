using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SanctionRepository : ISanctionRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SanctionRepository));

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
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    var entity = new Sancion
                    {
                        FechaSancion = dto.SanctionDateUtc,
                        TipoSancion = dto.SanctionType,
                        UsuarioIdUsuario = dto.UserId
                    };

                    context.Sancion.Add(entity);
                    context.SaveChanges();

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
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    var entity = context.Sancion.AsNoTracking()
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .OrderByDescending(s => s.FechaSancion)
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
                using (var context = new SnakeAndLaddersDBEntities1())
                {
                    return context.Sancion.AsNoTracking()
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .OrderByDescending(s => s.FechaSancion)
                        .Select(s => new SanctionDto
                        {
                            SanctionId = s.IdSancion,
                            SanctionDateUtc = s.FechaSancion,
                            SanctionType = s.TipoSancion,
                            UserId = s.UsuarioIdUsuario
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