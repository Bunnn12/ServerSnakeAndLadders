using System;
using System.Text.RegularExpressions;

namespace SnakesAndLadders.Server.Helpers
{
    /// <summary>
    /// Provides helper methods to normalize and validate avatar identifiers.
    /// </summary>
    public static class AvatarIdHelper
    {
        public const string DEFAULT_AVATAR_ID = "A0013";
        private const int AVATAR_ID_MAX_LENGTH = 5;
        private const string AVATAR_ID_PATTERN = @"^A\d{4}$";

        private static readonly Regex _avatarIdRegex = new Regex(
            AVATAR_ID_PATTERN,
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Normalizes an avatar identifier by trimming and converting it to upper case.
        /// Returns null if the value is null, empty, whitespace or exceeds the max length.
        /// </summary>
        /// <param name="id">Raw avatar identifier.</param>
        /// <returns>Normalized identifier or null if it is not acceptable.</returns>
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

        /// <summary>
        /// Determines whether the given avatar identifier is valid.
        /// </summary>
        /// <param name="id">Avatar identifier to validate.</param>
        /// <returns>True if the identifier matches the expected pattern; otherwise, false.</returns>
        public static bool IsValid(string id)
        {
            var normalizedId = NormalizeAvatarId(id);
            return normalizedId != null && _avatarIdRegex.IsMatch(normalizedId);
        }

        /// <summary>
        /// Maps a database avatar value to a valid application avatar identifier,
        /// falling back to the default avatar id when the value is invalid.
        /// </summary>
        /// <param name="dbValue">Avatar identifier coming from the database.</param>
        /// <returns>A valid avatar identifier, or the default avatar id if invalid.</returns>
        public static string MapFromDb(string dbValue)
        {
            var normalizedDbValue = NormalizeAvatarId(dbValue);
            return IsValid(normalizedDbValue) ? normalizedDbValue : DEFAULT_AVATAR_ID;
        }

        /// <summary>
        /// Maps an application avatar value to the database format.
        /// </summary>
        /// <param name="appValue">Avatar identifier coming from the application.</param>
        /// <returns>The normalized identifier or null if the input is null/whitespace.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the value does not match the expected pattern 'A' + 4 digits.
        /// </exception>
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
