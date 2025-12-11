using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SocialProfileRepository : ISocialProfileRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SocialProfileRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public SocialProfileRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IReadOnlyList<SocialProfileDto> GetByUserId(int userId)
        {
            SocialProfileRepositoryHelper.ValidateUserId(
                userId,
                nameof(userId),
                SocialProfileRepositoryConstants.ERROR_USER_ID_POSITIVE);

            try
            {
                using (SnakeAndLaddersDBEntities1 db = _contextFactory())
                {
                    SocialProfileRepositoryHelper.ConfigureContext(db);

                    List<RedesSociales> rows = db.RedesSociales
                        .AsNoTracking()
                        .Where(r => r.UsuarioIdUsuario == userId)
                        .ToList();

                    var result = new List<SocialProfileDto>();

                    foreach (RedesSociales row in rows)
                    {
                        SocialNetworkType network;

                        try
                        {
                            network = SocialProfileRepositoryHelper.ParseNetwork(row.TipoRedSocial);
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }

                        result.Add(new SocialProfileDto
                        {
                            UserId = row.UsuarioIdUsuario,
                            Network = network,
                            ProfileLink = row.LinkRedSocial
                        });
                    }

                    return result;
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_GET_BY_USER, ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_GET_BY_USER, ex);
                throw;
            }
        }

        public SocialProfileDto Upsert(LinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    SocialProfileRepositoryConstants.ERROR_REQUEST_NULL);
            }

            SocialProfileRepositoryHelper.ValidateUserId(
                request.UserId,
                nameof(request.UserId),
                SocialProfileRepositoryConstants.ERROR_USER_ID_POSITIVE);

            string profileLink = SocialProfileRepositoryHelper.NormalizeAndValidateProfileLink(request.ProfileLink);
            string networkCode = SocialProfileRepositoryHelper.GetNetworkCode(request.Network);

            try
            {
                using (SnakeAndLaddersDBEntities1 db = _contextFactory())
                {
                    SocialProfileRepositoryHelper.ConfigureContext(db);

                    RedesSociales entity = db.RedesSociales
                        .SingleOrDefault(r =>
                            r.UsuarioIdUsuario == request.UserId &&
                            r.TipoRedSocial == networkCode);

                    if (entity == null)
                    {
                        entity = db.RedesSociales.Create();
                        entity.UsuarioIdUsuario = request.UserId;
                        entity.TipoRedSocial = networkCode;
                        entity.LinkRedSocial = profileLink;

                        db.RedesSociales.Add(entity);
                    }
                    else
                    {
                        entity.LinkRedSocial = profileLink;
                    }

                    db.SaveChanges();

                    return new SocialProfileDto
                    {
                        UserId = entity.UsuarioIdUsuario,
                        Network = SocialProfileRepositoryHelper.ParseNetwork(entity.TipoRedSocial),
                        ProfileLink = entity.LinkRedSocial
                    };
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_UPSERT, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_UPSERT, ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_UPSERT, ex);
                throw;
            }
        }

        public void DeleteSocialNetwork(UnlinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    SocialProfileRepositoryConstants.ERROR_REQUEST_NULL);
            }

            SocialProfileRepositoryHelper.ValidateUserId(
                request.UserId,
                nameof(request.UserId),
                SocialProfileRepositoryConstants.ERROR_USER_ID_POSITIVE);

            string networkCode = SocialProfileRepositoryHelper.GetNetworkCode(request.Network);

            try
            {
                using (SnakeAndLaddersDBEntities1 db = _contextFactory())
                {
                    SocialProfileRepositoryHelper.ConfigureContext(db);

                    RedesSociales entity = db.RedesSociales
                        .SingleOrDefault(r =>
                            r.UsuarioIdUsuario == request.UserId &&
                            r.TipoRedSocial == networkCode);

                    if (entity == null)
                    {
                        return;
                    }

                    db.RedesSociales.Remove(entity);
                    db.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_DELETE, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_DELETE, ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(SocialProfileRepositoryConstants.LOG_ERROR_DELETE, ex);
                throw;
            }
        }
    }
}
