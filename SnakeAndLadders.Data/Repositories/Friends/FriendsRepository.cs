using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Interfaces;
using SnakesAndLadders.Data.Repositories.Friends;
using System;
using System.Collections.Generic;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class FriendsRepository : IFriendsRepository
    {
        private readonly IFriendRelationshipRepository _relationshipRepository;
        private readonly IFriendRequestRepository _requestRepository;
        private readonly IFriendSearchRepository _searchRepository;

        public FriendsRepository()
            : this(() => new SnakeAndLaddersDBEntities1())
        {
        }

        public FriendsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
            : this(
                new FriendRelationshipRepository(contextFactory),
                new FriendRequestRepository(contextFactory),
                new FriendSearchRepository(contextFactory))
        {
        }
        internal FriendsRepository(
            IFriendRelationshipRepository relationshipRepository,
            IFriendRequestRepository requestRepository,
            IFriendSearchRepository searchRepository)
        {
            _relationshipRepository = relationshipRepository
                ?? throw new ArgumentNullException(nameof(relationshipRepository));

            _requestRepository = requestRepository
                ?? throw new ArgumentNullException(nameof(requestRepository));

            _searchRepository = searchRepository
                ?? throw new ArgumentNullException(nameof(searchRepository));
        }
        public FriendLinkDto GetById(int friendLinkId)
        {
            return _relationshipRepository.GetById(friendLinkId);
        }

        public FriendLinkDto GetNormalized(int userIdA, int userIdB)
        {
            return _relationshipRepository.GetNormalized(userIdA, userIdB);
        }

        public FriendLinkDto CreatePending(int userIdA, int userIdB)
        {
            return _relationshipRepository.CreatePending(userIdA, userIdB);
        }

        public void UpdateStatus(int friendLinkId, byte newStatus)
        {
            _relationshipRepository.UpdateStatus(friendLinkId, newStatus);
        }

        public void DeleteLink(int friendLinkId)
        {
            _relationshipRepository.DeleteLink(friendLinkId);
        }

        public IReadOnlyList<int> GetAcceptedFriendsIds(int userId)
        {
            return _relationshipRepository.GetAcceptedFriendsIds(userId);
        }

        public IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId)
        {
            return _requestRepository.GetPendingRelated(userId);
        }

        public IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId)
        {
            return _requestRepository.GetAcceptedFriendsDetailed(userId);
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId)
        {
            return _requestRepository.GetIncomingPendingDetailed(userId);
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId)
        {
            return _requestRepository.GetOutgoingPendingDetailed(userId);
        }

        public IReadOnlyList<UserBriefDto> SearchUsers(string query, int maxResults, int excludeUserId)
        {
            return _searchRepository.SearchUsers(query, maxResults, excludeUserId);
        }
    }
}
