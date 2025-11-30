using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class InviteFriendToGameRequestDto
    {
        public string SessionToken { get; set; }
        public int FriendUserId { get; set; }
        public string GameCode { get; set; }
    }
}
