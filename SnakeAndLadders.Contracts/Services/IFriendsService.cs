using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IFriendsService
    {
        [OperationContract] FriendLinkDto SendFriendRequest(string token, int targetUserId);
        [OperationContract] void AcceptFriendRequest(string token, int friendLinkId);
        [OperationContract] void RejectFriendRequest(string token, int friendLinkId);
        [OperationContract] void CancelFriendRequest(string token, int friendLinkId);
        [OperationContract] void RemoveFriend(string token, int friendLinkId);

        [OperationContract] FriendLinkDto GetStatus(string token, int otherUserId);
        [OperationContract] List<int> GetFriendsIds(string token);
        [OperationContract] List<FriendLinkDto> GetIncomingPending(string token);
        [OperationContract] List<FriendLinkDto> GetOutgoingPending(string token);
        [OperationContract] List<FriendListItemDto> GetFriends(string token);
        [OperationContract] List<FriendRequestItemDto> GetIncomingRequests(string token);
        [OperationContract] List<FriendRequestItemDto> GetOutgoingRequests(string token);
        [OperationContract] List<UserBriefDto> SearchUsers(string token, string query, int maxResults);
    }
}
