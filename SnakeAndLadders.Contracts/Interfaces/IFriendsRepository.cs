using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IFriendsRepository
    {
        FriendLinkDto GetById(int friendLinkId);
        FriendLinkDto GetNormalized(int userIdA, int userIdB);
        FriendLinkDto CreatePending(int userIdA, int userIdB);
        void UpdateStatus(int friendLinkId, byte newStatus);
        void DeleteLink(int friendLinkId);

        IReadOnlyList<int> GetAcceptedFriendsIds(int userId);
        IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId);
    }
}
