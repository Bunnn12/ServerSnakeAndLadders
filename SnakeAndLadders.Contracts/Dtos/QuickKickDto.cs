using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class QuickKickDto
    {
        public int TargetUserId { get; set; }
        public int HostUserId { get; set; }
        public string KickReason { get; set; }
    }
}
