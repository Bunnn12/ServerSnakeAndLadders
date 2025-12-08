using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class SocialProfileRepository : ISocialProfileRepository
    {
        private const int MAX_PROFILE_LINK_LENGTH = 255;
        private const int MIN_VALID_USER_ID = 1;

        private const string NETWORK_INSTAGRAM = "INSTAGRAM";
        private const string NETWORK_FACEBOOK = "FACEBOOK";
        private const string NETWORK_TWITTER = "TWITTER";

        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";
        private const string ERROR_REQUEST_NULL = "Request cannot be null.";
        private const string ERROR_PROFILE_LINK_REQUIRED = "ProfileLink is required.";
        private const string ERROR_SOCIAL_TYPE_REQUIRED = "TipoRedSocial is required.";
        private const string ERROR_UNKNOWN_SOCIAL_NETWORK_TEMPLATE =
            "Unknown social network type: {0}";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public SocialProfileRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IReadOnlyList<SocialProfileDto> GetByUserId(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
                var rows = db.RedesSociales
                    .AsNoTracking()
                    .Where(r => r.UsuarioIdUsuario == userId)
                    .ToList();

                List<SocialProfileDto> result = new List<SocialProfileDto>();

                foreach (RedesSociales row in rows)
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
                throw new ArgumentNullException(nameof(request), ERROR_REQUEST_NULL);
            }

            if (request.UserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId), ERROR_USER_ID_POSITIVE);
            }

            string profileLink = NormalizeAndValidateProfileLink(request.ProfileLink);
            string networkCode = GetNetworkCode(request.Network);

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
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
                    Network = ParseNetwork(entity.TipoRedSocial),
                    ProfileLink = entity.LinkRedSocial
                };
            }
        }

        public void DeleteSocialNetwork(UnlinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), ERROR_REQUEST_NULL);
            }

            if (request.UserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(request.UserId), ERROR_USER_ID_POSITIVE);
            }

            string networkCode = GetNetworkCode(request.Network);

            using (SnakeAndLaddersDBEntities1 db = _contextFactory())
            {
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

        private static string NormalizeAndValidateProfileLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(ERROR_PROFILE_LINK_REQUIRED, nameof(value));
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
                throw new ArgumentException(ERROR_SOCIAL_TYPE_REQUIRED, nameof(value));
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

            string message = string.Format(
                ERROR_UNKNOWN_SOCIAL_NETWORK_TEMPLATE,
                normalized);

            throw new ArgumentException(message, nameof(value));
        }
    }
}
