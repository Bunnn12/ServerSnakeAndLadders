using System;
using System.Data.Entity.Infrastructure;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class SocialProfileRepositoryHelper
    {
        internal static void ValidateUserId(
            int userId,
            string paramName,
            string errorMessage)
        {
            if (userId < SocialProfileRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(paramName, errorMessage);
            }
        }

        internal static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                SocialProfileRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }

        internal static string NormalizeAndValidateProfileLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    SocialProfileRepositoryConstants.ERROR_PROFILE_LINK_REQUIRED,
                    nameof(value));
            }

            string trimmed = value.Trim();

            if (trimmed.Length > SocialProfileRepositoryConstants.MAX_PROFILE_LINK_LENGTH)
            {
                return trimmed.Substring(0, SocialProfileRepositoryConstants.MAX_PROFILE_LINK_LENGTH);
            }

            return trimmed;
        }

        internal static string GetNetworkCode(SocialNetworkType network)
        {
            switch (network)
            {
                case SocialNetworkType.Instagram:
                    return SocialProfileRepositoryConstants.NETWORK_INSTAGRAM;
                case SocialNetworkType.Facebook:
                    return SocialProfileRepositoryConstants.NETWORK_FACEBOOK;
                case SocialNetworkType.Twitter:
                    return SocialProfileRepositoryConstants.NETWORK_TWITTER;
                default:
                    throw new ArgumentOutOfRangeException(nameof(network));
            }
        }

        internal static SocialNetworkType ParseNetwork(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    SocialProfileRepositoryConstants.ERROR_SOCIAL_TYPE_REQUIRED,
                    nameof(value));
            }

            string normalized = value.Trim().ToUpperInvariant();

            if (string.Equals(
                    normalized,
                    SocialProfileRepositoryConstants.NETWORK_INSTAGRAM,
                    StringComparison.Ordinal))
            {
                return SocialNetworkType.Instagram;
            }

            if (string.Equals(
                    normalized,
                    SocialProfileRepositoryConstants.NETWORK_FACEBOOK,
                    StringComparison.Ordinal))
            {
                return SocialNetworkType.Facebook;
            }

            if (string.Equals(
                    normalized,
                    SocialProfileRepositoryConstants.NETWORK_TWITTER,
                    StringComparison.Ordinal))
            {
                return SocialNetworkType.Twitter;
            }

            string message = string.Format(
                SocialProfileRepositoryConstants.ERROR_UNKNOWN_SOCIAL_NETWORK_TEMPLATE,
                normalized);

            throw new ArgumentException(message, nameof(value));
        }
    }
}
