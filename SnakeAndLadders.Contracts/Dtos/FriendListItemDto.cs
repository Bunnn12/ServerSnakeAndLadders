using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class FriendListItemDto
    {
        public int FriendLinkId { get; set; }
        public int FriendUserId { get; set; }
        public string FriendUserName { get; set; } = string.Empty;
        public string ProfilePhotoId { get; set; }
    }
}
