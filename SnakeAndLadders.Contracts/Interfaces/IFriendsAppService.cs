using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IFriendsAppService
    {
        FriendLinkDto SendFriendRequest(string token, int targetUserId);
        void AcceptFriendRequest(string token, int friendLinkId);
        void RejectFriendRequest(string token, int friendLinkId);
        void CancelFriendRequest(string token, int friendLinkId);
        void RemoveFriend(string token, int friendLinkId);

        FriendLinkDto GetStatus(string token, int otherUserId);
        IReadOnlyList<int> GetFriendsIds(string token);
        IReadOnlyList<FriendLinkDto> GetIncomingPending(string token);
        IReadOnlyList<FriendLinkDto> GetOutgoingPending(string token);
    }
}
