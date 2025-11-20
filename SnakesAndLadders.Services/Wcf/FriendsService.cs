using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class FriendsService : IFriendsService
    {
        private readonly IFriendsAppService _friendsAppService;

        public FriendsService(IFriendsAppService friendsAppService)
        {
            _friendsAppService = friendsAppService ?? throw new ArgumentNullException(nameof(friendsAppService));
        }

        public FriendLinkDto SendFriendRequest(string token, int targetUserId)
        {
            return _friendsAppService.SendFriendRequest(token, targetUserId);
        }

        public void AcceptFriendRequest(string token, int friendLinkId)
        {
            _friendsAppService.AcceptFriendRequest(token, friendLinkId);
        }

        public void RejectFriendRequest(string token, int friendLinkId)
        {
            _friendsAppService.RejectFriendRequest(token, friendLinkId);
        }

        public void CancelFriendRequest(string token, int friendLinkId)
        {
            _friendsAppService.CancelFriendRequest(token, friendLinkId);
        }

        public void RemoveFriend(string token, int friendLinkId)
        {
            _friendsAppService.RemoveFriend(token, friendLinkId);
        }

        public FriendLinkDto GetStatus(string token, int otherUserId)
        {
            return _friendsAppService.GetStatus(token, otherUserId);
        }

        public List<int> GetFriendsIds(string token)
        {
            return new List<int>(_friendsAppService.GetFriendsIds(token));
        }

        public List<FriendLinkDto> GetIncomingPending(string token)
        {
            return new List<FriendLinkDto>(_friendsAppService.GetIncomingPending(token));
        }

        public List<FriendLinkDto> GetOutgoingPending(string token)
        {
            return new List<FriendLinkDto>(_friendsAppService.GetOutgoingPending(token));
        }

        public List<FriendListItemDto> GetFriends(string token)
        {
            return new List<FriendListItemDto>(_friendsAppService.GetFriends(token));
        }

        public List<FriendRequestItemDto> GetIncomingRequests(string token)
        {
            return new List<FriendRequestItemDto>(_friendsAppService.GetIncomingRequests(token));
        }

        public List<FriendRequestItemDto> GetOutgoingRequests(string token)
        {
            return new List<FriendRequestItemDto>(_friendsAppService.GetOutgoingRequests(token));
        }

        public List<UserBriefDto> SearchUsers(string token, string query, int maxResults)
        {
            return new List<UserBriefDto>(_friendsAppService.SearchUsers(token, query, maxResults));
        }
    }
}
