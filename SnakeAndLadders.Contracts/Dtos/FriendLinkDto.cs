using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class FriendLinkDto
    {
        public int FriendLinkId { get; set; }
        public int UserId1 { get; set; }   
        public int UserId2 { get; set; }   
        public FriendRequestStatus Status { get; set; }
    }
}
