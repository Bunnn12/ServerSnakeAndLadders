using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Repositories
{
    /// <summary>
    /// Repository for friend relationships: links, pending requests and friend search.
    /// </summary>
    public sealed class FriendsRepository : IFriendsRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsRepository));

        private static readonly byte[] _statusPendingBinaryValue = { 0x01 };
        private static readonly byte[] _statusAcceptedBinaryValue = { 0x02 };

        private const int COMMAND_TIMEOUT_SECONDS = 30;
        private const int DEFAULT_SEARCH_MAX_RESULTS = 20;
        private const int MAX_SEARCH_RESULTS = 100;

        public FriendLinkDto GetById(int friendLinkId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var entity = dbContext.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(friendLink => friendLink.IdListaAmigos == friendLinkId);

                return Map(entity);
            }
        }

        public FriendLinkDto GetNormalized(int userIdA, int userIdB)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var entity = dbContext.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(friendLink =>
                        (friendLink.UsuarioIdUsuario1 == userIdA && friendLink.UsuarioIdUsuario2 == userIdB) ||
                        (friendLink.UsuarioIdUsuario1 == userIdB && friendLink.UsuarioIdUsuario2 == userIdA));

                return Map(entity);
            }
        }

        /// <summary>
        /// Creates a pending friend link or auto-accepts when the reverse pending already exists.
        /// </summary>
        public FriendLinkDto CreatePending(int requesterUserId, int targetUserId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            using (var transaction = dbContext.Database.BeginTransaction())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var existingUsers = dbContext.Usuario
                    .Where(user =>
                        user.IdUsuario == requesterUserId ||
                        user.IdUsuario == targetUserId)
                    .Select(user => user.IdUsuario)
                    .ToList();

                if (existingUsers.Count != 2)
                {
                    throw new InvalidOperationException("Users not found.");
                }

                var existingLink = dbContext.ListaAmigos
                    .SingleOrDefault(friendLink =>
                        (friendLink.UsuarioIdUsuario1 == requesterUserId &&
                         friendLink.UsuarioIdUsuario2 == targetUserId) ||
                        (friendLink.UsuarioIdUsuario1 == targetUserId &&
                         friendLink.UsuarioIdUsuario2 == requesterUserId));

                if (existingLink == null)
                {
                    var newLink = new ListaAmigos
                    {
                        UsuarioIdUsuario1 = requesterUserId,
                        UsuarioIdUsuario2 = targetUserId,
                        EstadoSolicitud = _statusPendingBinaryValue
                    };

                    dbContext.ListaAmigos.Add(newLink);
                    dbContext.SaveChanges();
                    transaction.Commit();

                    return Map(newLink);
                }

                byte currentStatus = (existingLink.EstadoSolicitud != null &&
                                      existingLink.EstadoSolicitud.Length > 0)
                    ? existingLink.EstadoSolicitud[0]
                    : _statusPendingBinaryValue[0];

                if (currentStatus == _statusPendingBinaryValue[0])
                {
                    bool isReverse =
                        existingLink.UsuarioIdUsuario1 == targetUserId &&
                        existingLink.UsuarioIdUsuario2 == requesterUserId;

                    if (isReverse)
                    {
                        existingLink.EstadoSolicitud = _statusAcceptedBinaryValue;
                        dbContext.SaveChanges();
                        transaction.Commit();

                        return Map(existingLink);
                    }

                    throw new InvalidOperationException("Pending already exists.");
                }

                if (currentStatus == _statusAcceptedBinaryValue[0])
                {
                    throw new InvalidOperationException("Already friends.");
                }

                existingLink.UsuarioIdUsuario1 = requesterUserId;
                existingLink.UsuarioIdUsuario2 = targetUserId;
                existingLink.EstadoSolicitud = _statusPendingBinaryValue;

                dbContext.SaveChanges();
                transaction.Commit();

                return Map(existingLink);
            }
        }

        public void UpdateStatus(int friendLinkId, byte newStatus)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            using (var transaction = dbContext.Database.BeginTransaction())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var entity = dbContext.ListaAmigos
                    .SingleOrDefault(friendLink => friendLink.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    throw new InvalidOperationException();
                }

                entity.EstadoSolicitud = new[] { newStatus };

                dbContext.SaveChanges();
                transaction.Commit();
            }
        }

        public void DeleteLink(int friendLinkId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            using (var transaction = dbContext.Database.BeginTransaction())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var entity = dbContext.ListaAmigos
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
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var query = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId))
                    .Select(friendLink =>
                        friendLink.UsuarioIdUsuario1 == userId
                            ? friendLink.UsuarioIdUsuario2
                            : friendLink.UsuarioIdUsuario1);

                return query.ToList();
            }
        }

        public IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var query = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId));

                return query
                    .ToList()
                    .Select(Map)
                    .ToList();
            }
        }

        private static void NormalizePair(int valueA, int valueB, out int minValue, out int maxValue)
        {
            if (valueA < valueB)
            {
                minValue = valueA;
                maxValue = valueB;
            }
            else
            {
                minValue = valueB;
                maxValue = valueA;
            }
        }

        private static FriendLinkDto Map(ListaAmigos entity)
        {
            if (entity == null)
            {
                return null;
            }

            byte status = (entity.EstadoSolicitud != null &&
                           entity.EstadoSolicitud.Length > 0)
                ? entity.EstadoSolicitud[0]
                : _statusPendingBinaryValue[0];

            return new FriendLinkDto
            {
                FriendLinkId = entity.IdListaAmigos,
                UserId1 = entity.UsuarioIdUsuario1,
                UserId2 = entity.UsuarioIdUsuario2,
                Status = (FriendRequestStatus)status
            };
        }

        public IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

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

                var friendIds = rows
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

                return rows
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
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var pending = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == userId ||
                         friendLink.UsuarioIdUsuario2 == userId))
                    .ToList();

                // El que “recibe” es userId2
                var incoming = pending
                    .Where(friendLink => friendLink.UsuarioIdUsuario2 == userId)
                    .ToList();

                var involvedIds = incoming
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

                return incoming
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
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId)
        {
            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                var outgoing = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue &&
                        friendLink.UsuarioIdUsuario1 == userId)
                    .ToList();

                var involvedIds = outgoing
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

                return outgoing
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
            }
        }

        /// <summary>
        /// Searches users by user name, excluding the current user and users already linked.
        /// </summary>
        public IReadOnlyList<UserBriefDto> SearchUsers(string query, int maxResults, int excludeUserId)
        {
            query = (query ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<UserBriefDto>();
            }

            using (var dbContext = new SnakeAndLaddersDBEntities1())
            {
                ((IObjectContextAdapter)dbContext).ObjectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                int limit = maxResults > 0 && maxResults <= MAX_SEARCH_RESULTS
                    ? maxResults
                    : DEFAULT_SEARCH_MAX_RESULTS;

                var pendingTargets = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.UsuarioIdUsuario1 == excludeUserId &&
                        friendLink.EstadoSolicitud == _statusPendingBinaryValue)
                    .Select(friendLink => friendLink.UsuarioIdUsuario2);

                var acceptedTargets = dbContext.ListaAmigos
                    .AsNoTracking()
                    .Where(friendLink =>
                        friendLink.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (friendLink.UsuarioIdUsuario1 == excludeUserId ||
                         friendLink.UsuarioIdUsuario2 == excludeUserId))
                    .Select(friendLink =>
                        friendLink.UsuarioIdUsuario1 == excludeUserId
                            ? friendLink.UsuarioIdUsuario2
                            : friendLink.UsuarioIdUsuario1);

                var blockedIds = pendingTargets
                    .Concat(acceptedTargets)
                    .Distinct();

                var queryUsers = dbContext.Usuario
                    .AsNoTracking()
                    .Where(user =>
                        user.IdUsuario != excludeUserId &&
                        user.NombreUsuario.Contains(query) &&
                        !blockedIds.Contains(user.IdUsuario))
                    .OrderBy(user => user.NombreUsuario)
                    .Take(limit)
                    .Select(user => new UserBriefDto
                    {
                        UserId = user.IdUsuario,
                        UserName = user.NombreUsuario,
                        ProfilePhotoId = user.FotoPerfil
                    });

                return queryUsers.ToList();
            }
        }
    }
}
