using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class FriendRequestParticipants
    {
        public int RequesterUserId { get; }
        public int TargetUserId { get; }

        public FriendRequestParticipants(int requesterUserId, int targetUserId)
        {
            RequesterUserId = requesterUserId;
            TargetUserId = targetUserId;
        }
    }
}
