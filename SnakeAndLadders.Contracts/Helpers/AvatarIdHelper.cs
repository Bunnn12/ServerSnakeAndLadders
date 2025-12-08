using System;
using System.Text.RegularExpressions;

namespace SnakesAndLadders.Server.Helpers
{
    public static class AvatarIdHelper
    {
        public const string DEFAULT_AVATAR_ID = "A0013";
        private const int AVATAR_ID_MAX_LENGTH = 5;
        private const string AVATAR_ID_PATTERN = @"^A\d{4}$";

        private static readonly Regex _avatarIdRegex = new Regex(
            AVATAR_ID_PATTERN,
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string NormalizeAvatarId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var trimmed = id.Trim();
            if (trimmed.Length > AVATAR_ID_MAX_LENGTH)
            {
                return null;
            }

            return trimmed.ToUpperInvariant();
        }
        public static bool IsValid(string id)
        {
            var normalizedId = NormalizeAvatarId(id);
            return normalizedId != null && _avatarIdRegex.IsMatch(normalizedId);
        }

        public static string MapFromDb(string dbValue)
        {
            var normalizedDbValue = NormalizeAvatarId(dbValue);
            return IsValid(normalizedDbValue) ? normalizedDbValue : DEFAULT_AVATAR_ID;
        }

        public static string MapToDb(string appValue)
        {
            var normalizedAppValue = NormalizeAvatarId(appValue);
            if (normalizedAppValue == null)
            {
                return null;
            }

            if (!IsValid(normalizedAppValue))
            {
                throw new ArgumentException(
                    "El avatar id debe tener formato 'A' + 4 dígitos (ej. A0001).",
                    nameof(appValue));
            }

            return normalizedAppValue;
        }
    }
}
