using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class BanInfoDto
    {
        public bool IsBanned { get; set; }
        public string SanctionType { get; set; }
        public DateTime? BanEndsAtUtc { get; set; }

    }
}
