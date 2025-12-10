using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class KickPlayerFromLobbyRequest
    {
        public int LobbyId { get; set; }
        public int HostUserId { get; set; }
        public int TargetUserId { get; set; }
    }
}
