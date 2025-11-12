using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class FriendRequestItemDto
    {
        public int FriendLinkId { get; set; }
        public int RequesterUserId { get; set; }
        public string RequesterUserName { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string TargetUserName { get; set; } = string.Empty;
        public FriendRequestStatus Status { get; set; }
    }
}
