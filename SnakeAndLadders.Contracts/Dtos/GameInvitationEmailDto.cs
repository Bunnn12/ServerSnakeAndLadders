using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class GameInvitationEmailDto
    {
        public string ToEmail { get; set; }
        public string ToUserName { get; set; }
        public string InviterUserName { get; set; }
        public string GameCode { get; set; }
    }
}
