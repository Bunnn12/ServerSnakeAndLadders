using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    /// <summary>
    /// Application service that manages friend requests, links and search operations.
    /// </summary>
    public sealed class FriendsAppService : IFriendsAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsAppService));

        private const string CODE_INVALID_SESSION = "FRD_INVALID_SESSION";
        private const string CODE_SAME_USER = "FRD_SAME_USER";
        private const string CODE_LINK_EXISTS = "FRD_LINK_EXISTS";
        private const string CODE_LINK_NOT_FOUND = "FRD_LINK_NOT_FOUND";
        private const string CODE_NOT_IN_LINK = "FRD_NOT_IN_LINK";

        private const byte STATUS_PENDING = 0x01;
        private const byte STATUS_ACCEPTED = 0x02;
        private const byte STATUS_REJECTED = 0x03;

        private readonly IFriendsRepository _friendsRepository;
        private readonly Func<string, int> _getUserIdFromToken;

        public FriendsAppService(
            IFriendsRepository friendsRepository,
            Func<string, int> getUserIdFromToken)
        {
            _friendsRepository = friendsRepository ?? throw new ArgumentNullException(nameof(friendsRepository));
            _getUserIdFromToken = getUserIdFromToken ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        /// <summary>
        /// Sends a friend request from the current user (token) to the target user.
        /// May auto-accept if a reverse pending request exists.
        /// </summary>
        public FriendLinkDto SendFriendRequest(string token, int targetUserId)
        {
            int currentUserId = EnsureUser(token);

            if (currentUserId == targetUserId)
            {
                throw Faults.Create(CODE_SAME_USER, string.Empty);
            }

            try
            {
                FriendLinkDto link = _friendsRepository.CreatePending(currentUserId, targetUserId);

                if (link.Status == FriendRequestStatus.Accepted)
                {
                    Logger.InfoFormat(
                        "Friend auto-accepted due to cross-request. users {0} <-> {1}",
                        currentUserId,
                        targetUserId);
                }
                else
                {
                    Logger.InfoFormat(
                        "Friend request created/reopened. users {0} -> {1}",
                        currentUserId,
                        targetUserId);
                }

                return link;
            }
            catch (InvalidOperationException ex)
            {
                string message = ex.Message ?? string.Empty;

                if (message.Contains("Pending already exists"))
                {
                    throw Faults.Create(CODE_LINK_EXISTS, "There is already a pending request.");
                }

                if (message.Contains("Already friends"))
                {
                    throw Faults.Create(CODE_LINK_EXISTS, "You are already friends.");
                }

                throw;
            }
        }

        public void AcceptFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            if (!InvolvesUser(link, currentUserId))
            {
                throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);
            }

            _friendsRepository.UpdateStatus(friendLinkId, STATUS_ACCEPTED);
            Logger.InfoFormat("Friend request accepted. linkId {0}", friendLinkId);
        }

        public void RejectFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            if (!InvolvesUser(link, currentUserId))
            {
                throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);
            }

            _friendsRepository.UpdateStatus(friendLinkId, STATUS_REJECTED);
            Logger.InfoFormat("Friend request rejected. linkId {0}", friendLinkId);
        }

        public void CancelFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            if (!InvolvesUser(link, currentUserId))
            {
                throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);
            }

            _friendsRepository.DeleteLink(friendLinkId);
            Logger.InfoFormat("Friend request canceled. linkId {0}", friendLinkId);
        }

        public void RemoveFriend(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            if (!InvolvesUser(link, currentUserId))
            {
                throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);
            }

            _friendsRepository.DeleteLink(friendLinkId);
            Logger.InfoFormat("Friend removed. linkId {0}", friendLinkId);
        }

        public FriendLinkDto GetStatus(string token, int otherUserId)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.GetNormalized(currentUserId, otherUserId);
        }

        public IReadOnlyList<int> GetFriendsIds(string token)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.GetAcceptedFriendsIds(currentUserId);
        }

        public IReadOnlyList<FriendListItemDto> GetFriends(string token)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.GetAcceptedFriendsDetailed(currentUserId);
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingRequests(string token)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.GetIncomingPendingDetailed(currentUserId);
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingRequests(string token)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.GetOutgoingPendingDetailed(currentUserId);
        }

        public IReadOnlyList<UserBriefDto> SearchUsers(string token, string query, int maxResults)
        {
            int currentUserId = EnsureUser(token);
            return _friendsRepository.SearchUsers(query, maxResults, currentUserId);
        }

        public IReadOnlyList<FriendLinkDto> GetIncomingPending(string token)
        {
            int currentUserId = EnsureUser(token);

            return _friendsRepository
                .GetPendingRelated(currentUserId)
                .Where(link => link.UserId2 == currentUserId)
                .ToList();
        }

        public IReadOnlyList<FriendLinkDto> GetOutgoingPending(string token)
        {
            int currentUserId = EnsureUser(token);

            return _friendsRepository
                .GetPendingRelated(currentUserId)
                .Where(link => link.UserId1 == currentUserId)
                .ToList();
        }

        private int EnsureUser(string token)
        {
            int userId = _getUserIdFromToken(token);

            if (userId <= 0)
            {
                throw Faults.Create(CODE_INVALID_SESSION, string.Empty);
            }

            return userId;
        }

        private FriendLinkDto RequireLink(int friendLinkId)
        {
            FriendLinkDto link = _friendsRepository.GetById(friendLinkId);

            if (link == null)
            {
                throw Faults.Create(CODE_LINK_NOT_FOUND, string.Empty);
            }

            return link;
        }

        private static bool InvolvesUser(FriendLinkDto link, int userId)
        {
            return link != null && (link.UserId1 == userId || link.UserId2 == userId);
        }
    }
}
