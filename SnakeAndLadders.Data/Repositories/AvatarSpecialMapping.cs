using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories
{
    internal static class AvatarSpecialMapping
    {
        public static readonly IReadOnlyDictionary<int, string> SpecialAvatarCodesById =
            new Dictionary<int, string>
            {
                { 3,  "A0014" },
                { 2,  "A0015" },
                { 6,  "A0016" },
                { 5,  "A0017" },
                { 4,  "A0018" },
                { 1,  "A0019" },
               // { 0,  "A0020" },
               // { 0,  "A0021" },
               // { 0,  "A0022" },
                { 7, "A0023" }  
            };

        public static bool TryGetAvatarCode(int avatarEntityId, out string avatarCode)
        {
            return SpecialAvatarCodesById.TryGetValue(avatarEntityId, out avatarCode);
        }
    }
}
