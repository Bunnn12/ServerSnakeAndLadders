using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IFriendRequestRepository
    {
        IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId);
        IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId);
        IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId);
        IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId);
    }
}
