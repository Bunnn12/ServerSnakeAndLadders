using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SnakesAndLadders.Server.Helpers
{
    public static class AvatarIdHelper
    {
        public const string DefaultId = "DEF";

        private static readonly Regex Pattern = new Regex(@"^A\d{4}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Normalize(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? null
                : id.Trim().ToUpperInvariant();
        }

        public static bool IsValid(string id)
        {
            var norm = Normalize(id);
            return norm != null && Pattern.IsMatch(norm);
        }

        public static string MapFromDb(string dbValue)
        {
            var norm = Normalize(dbValue);
            return IsValid(norm) ? norm : DefaultId;
        }

        public static string MapToDb(string appValue)
        {
            var norm = Normalize(appValue);
            if (norm == null) return null; 
            if (!IsValid(norm))
                throw new ArgumentException("El avatar id debe tener formato 'A' + 4 dígitos (ej. A0001).", nameof(appValue));
            return norm;
        }
    }
}
