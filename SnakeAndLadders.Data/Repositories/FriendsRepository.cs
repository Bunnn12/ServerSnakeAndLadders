using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class FriendsRepository : IFriendsRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FriendsRepository));

        private const byte FRIEND_REQUEST_STATUS_PENDING_VALUE = 0x01;
        private const byte FRIEND_REQUEST_STATUS_ACCEPTED_VALUE = 0x02;

        private static readonly byte[] _statusPendingBinaryValue = { FRIEND_REQUEST_STATUS_PENDING_VALUE };
        private static readonly byte[] _statusAcceptedBinaryValue = { FRIEND_REQUEST_STATUS_ACCEPTED_VALUE };

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int DEFAULT_SEARCH_MAX_RESULTS = 20;
        private const int MAX_SEARCH_RESULTS = 100;
        private const int MIN_VALID_USER_ID = 1;
        private const int STATUS_MIN_LENGTH = 1;
        private const int STATUS_INDEX = 0;
        private const int EXPECTED_USER_COUNT_FOR_LINK = 2;
        private const int DEFAULT_FRIEND_LINK_ID = 0;
        private const int DEFAULT_USER_ID = 0;

        private const FriendRequestStatus DEFAULT_FRIEND_REQUEST_STATUS = FriendRequestStatus.Pending;

        private const string ERROR_USERS_NOT_FOUND = "Users not found.";
        private const string ERROR_PENDING_ALREADY_EXISTS = "Pending friend request already exists.";
        private const string ERROR_ALREADY_FRIENDS = "Users are already friends.";
        private const string ERROR_FRIEND_LINK_NOT_FOUND = "Friend link not found.";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public FriendsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public FriendLinkDto GetById(int friendLinkId)
        {
            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                ListaAmigos entity = dbContext.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(friendLink => friendLink.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    return CreateEmptyFriendLink();
                }

                FriendLinkDto dto = Map(entity);
                return dto;
            }
        }

        public FriendLinkDto GetNormalized(int userIdA, int userIdB)
        {
            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                ListaAmigos entity = dbContext.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(friendLink =>
                        (friendLink.UsuarioIdUsuario1 == userIdA && friendLink.UsuarioIdUsuario2 == userIdB) ||
                        (friendLink.UsuarioIdUsuario1 == userIdB && friendLink.UsuarioIdUsuario2 == userIdA));

                if (entity == null)
                {
                    return CreateEmptyFriendLink();
                }

                FriendLinkDto dto = Map(entity);
                return dto;
            }
        }

        public FriendLinkDto CreatePending(int requesterUserId, int targetUserId)
        {
            if (requesterUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(requesterUserId), ERROR_USER_ID_POSITIVE);
            }

            if (targetUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(targetUserId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            using (DbContextTransaction transaction = dbContext.Database.BeginTransaction())
            {
                ConfigureContext(dbContext);

                List<int> existingUsers = dbContext.Usuario
                    .Where(user =>
                        user.IdUsuario == requesterUserId ||
                        user.IdUsuario == targetUserId)
                    .Select(user => user.IdUsuario)
                    .ToList();

                if (existingUsers.Count != EXPECTED_USER_COUNT_FOR_LINK)
                {
                    throw new InvalidOperationException(ERROR_USERS_NOT_FOUND);
                }

                ListaAmigos existingLink = dbContext.ListaAmigos
                    .SingleOrDefault(friendLink =>
                        (friendLink.UsuarioIdUsuario1 == requesterUserId &&
                         friendLink.UsuarioIdUsuario2 == targetUserId) ||
                        (friendLink.UsuarioIdUsuario1 == targetUserId &&
                         friendLink.UsuarioIdUsuario2 == requesterUserId));

                if (existingLink == null)
                {
                    ListaAmigos newLink = new ListaAmigos
                    {
                        UsuarioIdUsuario1 = requesterUserId,
                        UsuarioIdUsuario2 = targetUserId,
                        EstadoSolicitud = _statusPendingBinaryValue
                    };

                    dbContext.ListaAmigos.Add(newLink);
                    dbContext.SaveChanges();
                    transaction.Commit();

                    FriendLinkDto dto = Map(newLink);
                    return dto;
                }

                byte currentStatus = GetStatusValue(existingLink.EstadoSolicitud);

                if (currentStatus == FRIEND_REQUEST_STATUS_PENDING_VALUE)
                {
                    bool isReverse =
                        existingLink.UsuarioIdUsuario1 == targetUserId &&
                        existingLink.UsuarioIdUsuario2 == requesterUserId;

                    if (isReverse)
                    {
                        existingLink.EstadoSolicitud = _statusAcceptedBinaryValue;
                        dbContext.SaveChanges();
                        transaction.Commit();

                        FriendLinkDto acceptedDto = Map(existingLink);
                        return acceptedDto;
                    }

                    throw new InvalidOperationException(ERROR_PENDING_ALREADY_EXISTS);
                }

                if (currentStatus == FRIEND_REQUEST_STATUS_ACCEPTED_VALUE)
                {
                    throw new InvalidOperationException(ERROR_ALREADY_FRIENDS);
                }

                existingLink.UsuarioIdUsuario1 = requesterUserId;
                existingLink.UsuarioIdUsuario2 = targetUserId;
                existingLink.EstadoSolicitud = _statusPendingBinaryValue;

                dbContext.SaveChanges();
                transaction.Commit();

                FriendLinkDto updatedDto = Map(existingLink);
                return updatedDto;
            }
        }

        public void UpdateStatus(int friendLinkId, byte newStatus)
        {
            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            using (DbContextTransaction transaction = dbContext.Database.BeginTransaction())
            {
                ConfigureContext(dbContext);

                ListaAmigos entity = dbContext.ListaAmigos
                    .SingleOrDefault(friendLink => friendLink.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    throw new InvalidOperationException(ERROR_FRIEND_LINK_NOT_FOUND);
                }

                entity.EstadoSolicitud = new[] { newStatus };

                dbContext.SaveChanges();
                transaction.Commit();
            }
        }

        public void DeleteLink(int friendLinkId)
        {
            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            using (DbContextTransaction transaction = dbContext.Database.BeginTransaction())
            {
                ConfigureContext(dbContext);

                ListaAmigos entity = dbContext.ListaAmigos
                    .SingleOrDefault(friendLink => friendLink.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    return;
                }

                dbContext.ListaAmigos.Remove(entity);
                dbContext.SaveChanges();
                transaction.Commit();
            }
        }

        public IReadOnlyList<int> GetAcceptedFriendsIds(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                IQueryable<int> query = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId))
                    .Select(friendLink =>
                        friendLink.UsuarioIdUsuario1 == userId
                            ? friendLink.UsuarioIdUsuario2
                            : friendLink.UsuarioIdUsuario1);

                List<int> friendIds = query.ToList();
                return friendIds;
            }
        }

        public IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                IQueryable<ListaAmigos> query = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId));

                List<FriendLinkDto> result = query
                    .ToList()
                    .Select(Map)
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                var rows = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId))
                    .Select(friendLink => new
                    {
                        friendLink.IdListaAmigos,
                        FriendUserId = friendLink.UsuarioIdUsuario1 == userId
                            ? friendLink.UsuarioIdUsuario2
                            : friendLink.UsuarioIdUsuario1
                    })
                    .ToList();

                List<int> friendIds = rows
                    .Select(row => row.FriendUserId)
                    .ToList();

                var users = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user => friendIds.Contains(user.IdUsuario))
                    .Select(user => new
                    {
                        user.IdUsuario,
                        user.NombreUsuario,
                        user.FotoPerfil
                    })
                    .ToList()
                    .ToDictionary(user => user.IdUsuario);

                List<FriendListItemDto> result = rows
                    .Select(row => new FriendListItemDto
                    {
                        FriendLinkId = row.IdListaAmigos,
                        FriendUserId = row.FriendUserId,
                        FriendUserName = users.TryGetValue(row.FriendUserId, out var user)
                            ? user.NombreUsuario
                            : string.Empty,
                        ProfilePhotoId = users.TryGetValue(row.FriendUserId, out var user2)
                            ? user2.FotoPerfil
                            : null
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                List<ListaAmigos> pending = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId))
                    .ToList();

                List<ListaAmigos> incoming = pending
                    .Where(friendLink => friendLink.UsuarioIdUsuario2 == userId)
                    .ToList();

                List<int> involvedIds = incoming
                    .Select(friendLink => friendLink.UsuarioIdUsuario1)
                    .Concat(incoming.Select(friendLink => friendLink.UsuarioIdUsuario2))
                    .Distinct()
                    .ToList();

                var users = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user => involvedIds.Contains(user.IdUsuario))
                    .Select(user => new
                    {
                        user.IdUsuario,
                        user.NombreUsuario
                    })
                    .ToList()
                    .ToDictionary(user => user.IdUsuario);

                List<FriendRequestItemDto> result = incoming
                    .Select(entity => new FriendRequestItemDto
                    {
                        FriendLinkId = entity.IdListaAmigos,
                        RequesterUserId = entity.UsuarioIdUsuario1,
                        RequesterUserName = users[entity.UsuarioIdUsuario1].NombreUsuario,
                        TargetUserId = entity.UsuarioIdUsuario2,
                        TargetUserName = users[entity.UsuarioIdUsuario2].NombreUsuario,
                        Status = FriendRequestStatus.Pending
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                List<ListaAmigos> outgoing = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        friendLink.UsuarioIdUsuario1 == userId)
                    .ToList();

                List<int> involvedIds = outgoing
                    .Select(friendLink => friendLink.UsuarioIdUsuario1)
                    .Concat(outgoing.Select(friendLink => friendLink.UsuarioIdUsuario2))
                    .Distinct()
                    .ToList();

                var users = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user => involvedIds.Contains(user.IdUsuario))
                    .Select(user => new
                    {
                        user.IdUsuario,
                        user.NombreUsuario
                    })
                    .ToList()
                    .ToDictionary(user => user.IdUsuario);

                List<FriendRequestItemDto> result = outgoing
                    .Select(entity => new FriendRequestItemDto
                    {
                        FriendLinkId = entity.IdListaAmigos,
                        RequesterUserId = entity.UsuarioIdUsuario1,
                        RequesterUserName = users[entity.UsuarioIdUsuario1].NombreUsuario,
                        TargetUserId = entity.UsuarioIdUsuario2,
                        TargetUserName = users[entity.UsuarioIdUsuario2].NombreUsuario,
                        Status = FriendRequestStatus.Pending
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<UserBriefDto> SearchUsers(string query, int maxResults, int excludeUserId)
        {
            if (excludeUserId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeUserId), ERROR_USER_ID_POSITIVE);
            }

            string normalizedQuery = (query ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return new List<UserBriefDto>();
            }

            using (SnakeAndLaddersDBEntities1 dbContext = _contextFactory())
            {
                ConfigureContext(dbContext);

                int limit = maxResults > 0 && maxResults <= MAX_SEARCH_RESULTS
                    ? maxResults
                    : DEFAULT_SEARCH_MAX_RESULTS;

                IQueryable<int> pendingTargets = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.UsuarioIdUsuario1 == excludeUserId &&
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue)
                    .Select(friendLink => friendLink.UsuarioIdUsuario2);

                IQueryable<int> acceptedTargets = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == excludeUserId ||
                         friendLink.UsuarioIdUsuario2 == excludeUserId))
                    .Select(friendLink =>
                        friendLink.UsuarioIdUsuario1 == excludeUserId
                            ? friendLink.UsuarioIdUsuario2
                            : friendLink.UsuarioIdUsuario1);

                IQueryable<int> blockedIds = pendingTargets
                    .Concat(acceptedTargets)
                    .Distinct();

                IQueryable<UserBriefDto> queryUsers = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user =>
                        user.IdUsuario != excludeUserId &&
                        user.NombreUsuario.Contains(normalizedQuery) &&
                        !blockedIds.Contains(user.IdUsuario))
                    .OrderBy(user => user.NombreUsuario)
                    .Take(limit)
                    .Select(user => new UserBriefDto
                    {
                        UserId = user.IdUsuario,
                        UserName = user.NombreUsuario,
                        ProfilePhotoId = user.FotoPerfil
                    });

                List<UserBriefDto> result = queryUsers.ToList();
                return result;
            }
        }

        private static FriendLinkDto Map(ListaAmigos entity)
        {
            byte statusValue = GetStatusValue(entity.EstadoSolicitud);

            FriendLinkDto dto = new FriendLinkDto
            {
                FriendLinkId = entity.IdListaAmigos,
                UserId1 = entity.UsuarioIdUsuario1,
                UserId2 = entity.UsuarioIdUsuario2,
                Status = (FriendRequestStatus)statusValue
            };

            return dto;
        }

        private static byte GetStatusValue(byte[] statusBinary)
        {
            if (statusBinary == null || statusBinary.Length < STATUS_MIN_LENGTH)
            {
                return FRIEND_REQUEST_STATUS_PENDING_VALUE;
            }

            return statusBinary[STATUS_INDEX];
        }

        private static FriendLinkDto CreateEmptyFriendLink()
        {
            FriendLinkDto dto = new FriendLinkDto
            {
                FriendLinkId = DEFAULT_FRIEND_LINK_ID,
                UserId1 = DEFAULT_USER_ID,
                UserId2 = DEFAULT_USER_ID,
                Status = DEFAULT_FRIEND_REQUEST_STATUS
            };

            return dto;
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
