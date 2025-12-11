using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace SnakesAndLadders.Data.Repositories.Friends
{
    internal sealed class FriendRequestRepository : IFriendRequestRepository
    {
        private static readonly byte[] StatusPendingBinaryValue =
        {
            FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_PENDING_VALUE
        };

        private static readonly byte[] StatusAcceptedBinaryValue =
        {
            FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_ACCEPTED_VALUE
        };

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public FriendRequestRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId)
        {
            ValidateUserId(userId, nameof(userId));

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                List<ListaAmigos> pendingLinks = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == StatusPendingBinaryValue &&
                        (link.UsuarioIdUsuario1 == userId ||
                         link.UsuarioIdUsuario2 == userId))
                    .ToList();

                List<FriendLinkDto> result = pendingLinks
                    .Select(entity => new FriendLinkDto
                    {
                        FriendLinkId = entity.IdListaAmigos,
                        UserId1 = entity.UsuarioIdUsuario1,
                        UserId2 = entity.UsuarioIdUsuario2,
                        Status = FriendRequestStatus.Pending
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId)
        {
            ValidateUserId(userId, nameof(userId));

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                var rows = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == StatusAcceptedBinaryValue &&
                        (link.UsuarioIdUsuario1 == userId ||
                         link.UsuarioIdUsuario2 == userId))
                    .Select(link => new
                    {
                        link.IdListaAmigos,
                        FriendUserId = link.UsuarioIdUsuario1 == userId
                            ? link.UsuarioIdUsuario2
                            : link.UsuarioIdUsuario1
                    })
                    .ToList();

                List<int> friendIds = rows
                    .Select(row => row.FriendUserId)
                    .ToList();

                Dictionary<int, (string UserName, string PhotoId)> usersById =
                    LoadUsersWithPhoto(context, friendIds);

                List<FriendListItemDto> result = rows
                    .Select(row =>
                    {
                        bool found = usersById.TryGetValue(row.FriendUserId, out var user);
                        return new FriendListItemDto
                        {
                            FriendLinkId = row.IdListaAmigos,
                            FriendUserId = row.FriendUserId,
                            FriendUserName = found ? user.UserName : string.Empty,
                            ProfilePhotoId = found ? user.PhotoId : null
                        };
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId)
        {
            ValidateUserId(userId, nameof(userId));

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                List<ListaAmigos> pending = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == StatusPendingBinaryValue &&
                        (link.UsuarioIdUsuario1 == userId ||
                         link.UsuarioIdUsuario2 == userId))
                    .ToList();

                List<ListaAmigos> incoming = pending
                    .Where(link => link.UsuarioIdUsuario2 == userId)
                    .ToList();

                List<int> involvedIds = incoming
                    .Select(link => link.UsuarioIdUsuario1)
                    .Concat(incoming.Select(link => link.UsuarioIdUsuario2))
                    .Distinct()
                    .ToList();

                Dictionary<int, string> usersById =
                    LoadUsersWithoutPhoto(context, involvedIds);

                List<FriendRequestItemDto> result = incoming
                    .Select(entity => new FriendRequestItemDto
                    {
                        FriendLinkId = entity.IdListaAmigos,
                        RequesterUserId = entity.UsuarioIdUsuario1,
                        RequesterUserName = usersById.TryGetValue(entity.UsuarioIdUsuario1, out string requesterName)
                            ? requesterName
                            : string.Empty,
                        TargetUserId = entity.UsuarioIdUsuario2,
                        TargetUserName = usersById.TryGetValue(entity.UsuarioIdUsuario2, out string targetName)
                            ? targetName
                            : string.Empty,
                        Status = FriendRequestStatus.Pending
                    })
                    .ToList();

                return result;
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId)
        {
            ValidateUserId(userId, nameof(userId));

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                List<ListaAmigos> outgoing = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == StatusPendingBinaryValue &&
                        link.UsuarioIdUsuario1 == userId)
                    .ToList();

                List<int> involvedIds = outgoing
                    .Select(link => link.UsuarioIdUsuario1)
                    .Concat(outgoing.Select(link => link.UsuarioIdUsuario2))
                    .Distinct()
                    .ToList();

                Dictionary<int, string> usersById =
                    LoadUsersWithoutPhoto(context, involvedIds);

                List<FriendRequestItemDto> result = outgoing
                    .Select(entity => new FriendRequestItemDto
                    {
                        FriendLinkId = entity.IdListaAmigos,
                        RequesterUserId = entity.UsuarioIdUsuario1,
                        RequesterUserName = usersById.TryGetValue(entity.UsuarioIdUsuario1, out string requesterName)
                            ? requesterName
                            : string.Empty,
                        TargetUserId = entity.UsuarioIdUsuario2,
                        TargetUserName = usersById.TryGetValue(entity.UsuarioIdUsuario2, out string targetName)
                            ? targetName
                            : string.Empty,
                        Status = FriendRequestStatus.Pending
                    })
                    .ToList();

                return result;
            }
        }

        // ----------------- helpers -----------------

        private static void ValidateUserId(int userId, string paramName)
        {
            if (userId < FriendsRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    FriendsRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }
        }

        private static Dictionary<int, (string UserName, string PhotoId)> LoadUsersWithPhoto(
            SnakeAndLaddersDBEntities1 context,
            IReadOnlyCollection<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return new Dictionary<int, (string UserName, string PhotoId)>();
            }

            return context.Usuario
                .AsNoTracking()
                .Where(user => userIds.Contains(user.IdUsuario))
                .Select(user => new
                {
                    user.IdUsuario,
                    user.NombreUsuario,
                    user.FotoPerfil
                })
                .ToList()
                .ToDictionary(
                    u => u.IdUsuario,
                    u => (u.NombreUsuario, u.FotoPerfil));
        }

        private static Dictionary<int, string> LoadUsersWithoutPhoto(
            SnakeAndLaddersDBEntities1 context,
            IReadOnlyCollection<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return context.Usuario
                .AsNoTracking()
                .Where(user => userIds.Contains(user.IdUsuario))
                .Select(user => new
                {
                    user.IdUsuario,
                    user.NombreUsuario
                })
                .ToList()
                .ToDictionary(
                    u => u.IdUsuario,
                    u => u.NombreUsuario);
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                FriendsRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }
    }
}
