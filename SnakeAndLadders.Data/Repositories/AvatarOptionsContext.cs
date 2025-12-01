using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories
{
    internal class AvatarOptionsContext
    {
        
            public string NormalizedPhotoId { get; set; }

            public IList<int> UnlockedAvatarIds { get; set; }

            public int CurrentAvatarEntityId { get; set; }

            public IList<Avatar> AvatarEntities { get; set; }
        
    }
}
