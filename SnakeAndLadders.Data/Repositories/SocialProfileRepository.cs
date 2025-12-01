using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SocialProfileRepository : ISocialProfileRepository
    {
        private const int MAX_PROFILE_LINK_LENGTH = 510;

        private const string NETWORK_INSTAGRAM = "INSTAGRAM";
        private const string NETWORK_FACEBOOK = "FACEBOOK";
        private const string NETWORK_TWITTER = "TWITTER";

        public IReadOnlyList<SocialProfileDto> GetByUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var rows = db.RedesSociales
                    .AsNoTracking()
                    .Where(r => r.UsuarioIdUsuario == userId)
                    .ToList();

                List<SocialProfileDto> result = new List<SocialProfileDto>();

                foreach (var row in rows)
                {
                    SocialNetworkType network;

                    try
                    {
                        network = ParseNetwork(row.TipoRedSocial);
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


        public SocialProfileDto Upsert(LinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId));
            }

            string profileLink = NormalizeAndValidateProfileLink(request.ProfileLink);
            string networkCode = GetNetworkCode(request.Network);

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var entity = db.RedesSociales
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
                    Network = ParseNetwork(entity.TipoRedSocial),
                    ProfileLink = entity.LinkRedSocial
                };
            }
        }

        public void Delete(UnlinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.UserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId));
            }

            string networkCode = GetNetworkCode(request.Network);

            using (var db = new SnakeAndLaddersDBEntities1())
            {
                var entity = db.RedesSociales
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

        private static string NormalizeAndValidateProfileLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ProfileLink is required.", nameof(value));
            }

            string trimmed = value.Trim();

            if (trimmed.Length > MAX_PROFILE_LINK_LENGTH)
            {
                return trimmed.Substring(0, MAX_PROFILE_LINK_LENGTH);
            }

            return trimmed;
        }

        private static string GetNetworkCode(SocialNetworkType network)
        {
            switch (network)
            {
                case SocialNetworkType.Instagram:
                    return NETWORK_INSTAGRAM;
                case SocialNetworkType.Facebook:
                    return NETWORK_FACEBOOK;
                case SocialNetworkType.Twitter:
                    return NETWORK_TWITTER;
                default:
                    throw new ArgumentOutOfRangeException(nameof(network));
            }
        }

        private static SocialNetworkType ParseNetwork(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("TipoRedSocial is required.", nameof(value));
            }

            string normalized = value.Trim().ToUpperInvariant();

            if (string.Equals(normalized, NETWORK_INSTAGRAM, StringComparison.Ordinal))
            {
                return SocialNetworkType.Instagram;
            }

            if (string.Equals(normalized, NETWORK_FACEBOOK, StringComparison.Ordinal))
            {
                return SocialNetworkType.Facebook;
            }

            if (string.Equals(normalized, NETWORK_TWITTER, StringComparison.Ordinal))
            {
                return SocialNetworkType.Twitter;
            }

            throw new ArgumentException("Unknown social network type: " + normalized, nameof(value));
        }
    }
}
