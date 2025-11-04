using System;
using System.Text.RegularExpressions;

namespace SnakesAndLadders.Server.Helpers
{
    public static class AvatarIdHelper
    {
        public const string DefaultId = "A0013";

        private const int MaxLength = 5;

        private static readonly Regex Pattern = new Regex(
            @"^A\d{4}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Normalize(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var trimmed = id.Trim();
            if (trimmed.Length > MaxLength)
            {
                return null;
            }

            return trimmed.ToUpperInvariant();
        }

        public static bool IsValid(string id)
        {
            var normalizedId = Normalize(id);
            return normalizedId != null && Pattern.IsMatch(normalizedId);
        }

        public static string MapFromDb(string dbValue)
        {
            var normalizedDbValue = Normalize(dbValue);
            return IsValid(normalizedDbValue) ? normalizedDbValue : DefaultId;
        }

        public static string MapToDb(string appValue)
        {
            var normalizedAppValue = Normalize(appValue);
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
