using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IFriendRelationshipRepository
    {
        FriendLinkDto GetById(int friendLinkId);
        FriendLinkDto GetNormalized(int userIdA, int userIdB);
        FriendLinkDto CreatePending(int userIdA, int userIdB);
        void UpdateStatus(int friendLinkId, byte newStatus);
        void DeleteLink(int friendLinkId);
        IReadOnlyList<int> GetAcceptedFriendsIds(int userId);
    }
}
