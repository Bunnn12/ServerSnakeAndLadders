using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class FriendsAppService : IFriendsAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsAppService));

        private readonly IFriendsRepository _friendsRepository;
        private readonly Func<string, int> _getUserIdFromToken;

        public FriendsAppService(
            IFriendsRepository friendsRepository,
            Func<string, int> getUserIdFromToken)
        {
            _friendsRepository = friendsRepository
                                 ?? throw new ArgumentNullException(nameof(friendsRepository));
            _getUserIdFromToken = getUserIdFromToken
                                  ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public FriendLinkDto SendFriendRequest(string token, int targetUserId)
        {
            int currentUserId = EnsureUser(token);

            if (currentUserId == targetUserId)
            {
                throw Faults.Create(
                    FriendsAppServiceConstants.CODE_SAME_USER,
                    string.Empty);
            }

            try
            {
                FriendLinkDto link = _friendsRepository.CreatePending(currentUserId, targetUserId);

                if (link.Status == FriendRequestStatus.Accepted)
                {
                    Logger.InfoFormat(
                        FriendsAppServiceConstants.LOG_INFO_AUTO_ACCEPT_FORMAT,
                        currentUserId,
                        targetUserId);
                }
                else
                {
                    Logger.InfoFormat(
                        FriendsAppServiceConstants.LOG_INFO_REQUEST_CREATED_FORMAT,
                        currentUserId,
                        targetUserId);
                }

                return link;
            }
            catch (InvalidOperationException ex)
            {
                string message = ex.Message ?? string.Empty;

                if (message.Contains(FriendsAppServiceConstants.EX_MESSAGE_PENDING_ALREADY_EXISTS))
                {
                    throw Faults.Create(
                        FriendsAppServiceConstants.CODE_LINK_EXISTS,
                        FriendsAppServiceConstants.FAULT_MESSAGE_PENDING_EXISTS);
                }

                if (message.Contains(FriendsAppServiceConstants.EX_MESSAGE_ALREADY_FRIENDS))
                {
                    throw Faults.Create(
                        FriendsAppServiceConstants.CODE_LINK_EXISTS,
                        FriendsAppServiceConstants.FAULT_MESSAGE_ALREADY_FRIENDS);
                }

                throw;
            }
        }

        public void AcceptFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            EnsureUserInLink(link, currentUserId);

            _friendsRepository.UpdateStatus(friendLinkId, FriendsAppServiceConstants.STATUS_ACCEPTED);

            Logger.InfoFormat(
                FriendsAppServiceConstants.LOG_INFO_ACCEPTED_FORMAT,
                friendLinkId);
        }

        public void RejectFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            EnsureUserInLink(link, currentUserId);

            _friendsRepository.UpdateStatus(friendLinkId, FriendsAppServiceConstants.STATUS_REJECTED);

            Logger.InfoFormat(
                FriendsAppServiceConstants.LOG_INFO_REJECTED_FORMAT,
                friendLinkId);
        }

        public void CancelFriendRequest(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            EnsureUserInLink(link, currentUserId);

            _friendsRepository.DeleteLink(friendLinkId);

            Logger.InfoFormat(
                FriendsAppServiceConstants.LOG_INFO_CANCELED_FORMAT,
                friendLinkId);
        }

        public void RemoveFriend(string token, int friendLinkId)
        {
            int currentUserId = EnsureUser(token);
            FriendLinkDto link = RequireLink(friendLinkId);

            EnsureUserInLink(link, currentUserId);

            _friendsRepository.DeleteLink(friendLinkId);

            Logger.InfoFormat(
                FriendsAppServiceConstants.LOG_INFO_REMOVED_FORMAT,
                friendLinkId);
        }

        public FriendLinkDto GetStatus(string token, int otherUserId)
        {
            int currentUserId = EnsureUser(token);

            FriendLinkDto link = _friendsRepository.GetNormalized(currentUserId, otherUserId);

            if (link == null)
            {
                return new FriendLinkDto();
            }

            return link;
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

            string effectiveQuery = query ?? string.Empty;
            int effectiveMaxResults = maxResults <= 0
                ? FriendsAppServiceConstants.DEFAULT_SEARCH_MAX_RESULTS
                : maxResults;

            return _friendsRepository.SearchUsers(effectiveQuery, effectiveMaxResults, currentUserId);
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
                throw Faults.Create(
                    FriendsAppServiceConstants.CODE_INVALID_SESSION,
                    string.Empty);
            }

            return userId;
        }

        private FriendLinkDto RequireLink(int friendLinkId)
        {
            FriendLinkDto link = _friendsRepository.GetById(friendLinkId);

            if (link == null)
            {
                throw Faults.Create(
                    FriendsAppServiceConstants.CODE_LINK_NOT_FOUND,
                    string.Empty);
            }

            return link;
        }

        private static void EnsureUserInLink(FriendLinkDto link, int userId)
        {
            if (!InvolvesUser(link, userId))
            {
                throw Faults.Create(
                    FriendsAppServiceConstants.CODE_NOT_IN_LINK,
                    string.Empty);
            }
        }

        private static bool InvolvesUser(FriendLinkDto link, int userId)
        {
            return link != null
                   && (link.UserId1 == userId || link.UserId2 == userId);
        }
    }
}
