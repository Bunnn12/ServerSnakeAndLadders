using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Enums
{
    public enum FriendRequestStatus : byte
    {
        Pending = 0x01,
        Accepted = 0x02,
        Rejected = 0x03
    }
}
