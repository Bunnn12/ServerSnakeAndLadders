using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
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

        private readonly IFriendsRepository repository;
        private readonly Func<string, int> getUserIdFromToken; 

        public FriendsAppService(
            IFriendsRepository repository,
            Func<string, int> getUserIdFromToken)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.getUserIdFromToken = getUserIdFromToken ?? throw new ArgumentNullException(nameof(getUserIdFromToken));
        }

        public FriendLinkDto SendFriendRequest(string token, int targetUserId)
        {
            var current = EnsureUser(token);
            if (current == targetUserId) throw Faults.Create(CODE_SAME_USER, string.Empty);

            var existing = repository.GetNormalized(current, targetUserId);
            if (existing != null) throw Faults.Create(CODE_LINK_EXISTS, string.Empty);

            var link = repository.CreatePending(current, targetUserId);
            Logger.InfoFormat("Friend request created. users {0} & {1}", link.UserId1, link.UserId2);
            return link;
        }

        public void AcceptFriendRequest(string token, int friendLinkId)
        {
            var current = EnsureUser(token);
            var link = RequireLink(friendLinkId);
            if (!InvolvesUser(link, current)) throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);

            repository.UpdateStatus(friendLinkId, STATUS_ACCEPTED);
            Logger.InfoFormat("Friend request accepted. linkId {0}", friendLinkId);
        }

        public void RejectFriendRequest(string token, int friendLinkId)
        {
            var current = EnsureUser(token);
            var link = RequireLink(friendLinkId);
            if (!InvolvesUser(link, current)) throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);

            repository.UpdateStatus(friendLinkId, STATUS_REJECTED);
            Logger.InfoFormat("Friend request rejected. linkId {0}", friendLinkId);
        }

        public void CancelFriendRequest(string token, int friendLinkId)
        {
            var current = EnsureUser(token);
            var link = RequireLink(friendLinkId);
            if (!InvolvesUser(link, current)) throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);

            repository.DeleteLink(friendLinkId);
            Logger.InfoFormat("Friend request canceled. linkId {0}", friendLinkId);
        }

        public void RemoveFriend(string token, int friendLinkId)
        {
            var current = EnsureUser(token);
            var link = RequireLink(friendLinkId);
            if (!InvolvesUser(link, current)) throw Faults.Create(CODE_NOT_IN_LINK, string.Empty);

            repository.DeleteLink(friendLinkId);
            Logger.InfoFormat("Friend removed. linkId {0}", friendLinkId);
        }

        public FriendLinkDto GetStatus(string token, int otherUserId)
        {
            var current = EnsureUser(token);
            return repository.GetNormalized(current, otherUserId);
        }

        public IReadOnlyList<int> GetFriendsIds(string token)
        {
            var current = EnsureUser(token);
            return repository.GetAcceptedFriendsIds(current);
        }

        public IReadOnlyList<FriendLinkDto> GetIncomingPending(string token)
        {
            var current = EnsureUser(token);
            return repository.GetPendingRelated(current).ToList();
        }

        public IReadOnlyList<FriendLinkDto> GetOutgoingPending(string token)
        {
            var current = EnsureUser(token);
            return repository.GetPendingRelated(current).ToList();
        }

        private int EnsureUser(string token)
        {
            var userId = getUserIdFromToken(token);
            if (userId <= 0) throw Faults.Create(CODE_INVALID_SESSION, string.Empty);
            return userId;
        }

        private FriendLinkDto RequireLink(int friendLinkId)
        {
            var link = repository.GetById(friendLinkId);
            if (link == null) throw Faults.Create(CODE_LINK_NOT_FOUND, string.Empty);
            return link;
        }

        private static bool InvolvesUser(FriendLinkDto link, int userId)
            => link != null && (link.UserId1 == userId || link.UserId2 == userId);
    }
}
