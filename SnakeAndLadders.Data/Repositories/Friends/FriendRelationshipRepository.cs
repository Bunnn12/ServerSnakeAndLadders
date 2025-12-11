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
    internal sealed class FriendRelationshipRepository : IFriendRelationshipRepository
    {
        private static readonly byte[] _statusPendingBinaryValue =
        {
            FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_PENDING_VALUE
        };

        private static readonly byte[] _statusAcceptedBinaryValue =
        {
            FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_ACCEPTED_VALUE
        };

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public FriendRelationshipRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public FriendLinkDto GetById(int friendLinkId)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                ListaAmigos entity = context.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(link => link.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    return CreateEmptyFriendLink();
                }

                return Map(entity);
            }
        }

        public FriendLinkDto GetNormalized(int userIdA, int userIdB)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                ListaAmigos entity = context.ListaAmigos
                    .AsNoTracking()
                    .SingleOrDefault(link =>
                        (link.UsuarioIdUsuario1 == userIdA &&
                         link.UsuarioIdUsuario2 == userIdB) ||
                        (link.UsuarioIdUsuario1 == userIdB &&
                         link.UsuarioIdUsuario2 == userIdA));

                if (entity == null)
                {
                    return CreateEmptyFriendLink();
                }

                return Map(entity);
            }
        }

        public FriendLinkDto CreatePending(int userIdA, int userIdB)
        {
            ValidateUserId(userIdA, nameof(userIdA));
            ValidateUserId(userIdB, nameof(userIdB));

            var participants = new FriendRequestParticipants(userIdA, userIdB);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                ConfigureContext(context);

                EnsureUsersExist(context, participants);

                ListaAmigos existingLink = GetExistingLink(context, participants);

                ListaAmigos linkEntity;

                if (existingLink == null)
                {
                    linkEntity = CreateNewPendingLink(context, participants);
                }
                else
                {
                    linkEntity = UpdateExistingLink(
                        context,
                        existingLink,
                        participants);
                }

                context.SaveChanges();
                transaction.Commit();

                return Map(linkEntity);
            }
        }

        public void UpdateStatus(int friendLinkId, byte newStatus)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                ConfigureContext(context);

                ListaAmigos entity = context.ListaAmigos
                    .SingleOrDefault(link => link.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    throw new InvalidOperationException(
                        FriendsRepositoryConstants.ERROR_FRIEND_LINK_NOT_FOUND);
                }

                entity.EstadoSolicitud = new[] { newStatus };

                context.SaveChanges();
                transaction.Commit();
            }
        }

        public void DeleteLink(int friendLinkId)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                ConfigureContext(context);

                ListaAmigos entity = context.ListaAmigos
                    .SingleOrDefault(link => link.IdListaAmigos == friendLinkId);

                if (entity == null)
                {
                    return;
                }

                context.ListaAmigos.Remove(entity);
                context.SaveChanges();
                transaction.Commit();
            }
        }

        public IReadOnlyList<int> GetAcceptedFriendsIds(int userId)
        {
            ValidateUserId(userId, nameof(userId));

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                IQueryable<int> query = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (link.UsuarioIdUsuario1 == userId ||
                         link.UsuarioIdUsuario2 == userId))
                    .Select(link =>
                        link.UsuarioIdUsuario1 == userId
                            ? link.UsuarioIdUsuario2
                            : link.UsuarioIdUsuario1);

                return query.ToList();
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

        private static void EnsureUsersExist(
            SnakeAndLaddersDBEntities1 context,
            FriendRequestParticipants participants)
        {
            List<int> existingUsers = context.Usuario
                .Where(user =>
                    user.IdUsuario == participants.RequesterUserId ||
                    user.IdUsuario == participants.TargetUserId)
                .Select(user => user.IdUsuario)
                .ToList();

            if (existingUsers.Count !=
                FriendsRepositoryConstants.EXPECTED_USER_COUNT_FOR_LINK)
            {
                throw new InvalidOperationException(
                    FriendsRepositoryConstants.ERROR_USERS_NOT_FOUND);
            }
        }

        private static ListaAmigos GetExistingLink(
            SnakeAndLaddersDBEntities1 context,
            FriendRequestParticipants participants)
        {
            return context.ListaAmigos
                .SingleOrDefault(link =>
                    (link.UsuarioIdUsuario1 == participants.RequesterUserId &&
                     link.UsuarioIdUsuario2 == participants.TargetUserId) ||
                    (link.UsuarioIdUsuario1 == participants.TargetUserId &&
                     link.UsuarioIdUsuario2 == participants.RequesterUserId));
        }

        private static ListaAmigos CreateNewPendingLink(
            SnakeAndLaddersDBEntities1 context,
            FriendRequestParticipants participants)
        {
            var newLink = new ListaAmigos
            {
                UsuarioIdUsuario1 = participants.RequesterUserId,
                UsuarioIdUsuario2 = participants.TargetUserId,
                EstadoSolicitud = _statusPendingBinaryValue
            };

            context.ListaAmigos.Add(newLink);

            return newLink;
        }

        private static ListaAmigos UpdateExistingLink(
            SnakeAndLaddersDBEntities1 context,
            ListaAmigos existingLink,
            FriendRequestParticipants participants)
        {
            byte currentStatus = GetStatusValue(existingLink.EstadoSolicitud);

            if (currentStatus ==
                FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_PENDING_VALUE)
            {
                bool isReverse =
                    existingLink.UsuarioIdUsuario1 ==
                        participants.TargetUserId &&
                    existingLink.UsuarioIdUsuario2 ==
                        participants.RequesterUserId;

                if (isReverse)
                {
                    existingLink.EstadoSolicitud = _statusAcceptedBinaryValue;
                    return existingLink;
                }

                throw new InvalidOperationException(
                    FriendsRepositoryConstants.ERROR_PENDING_ALREADY_EXISTS);
            }

            if (currentStatus ==
                FriendsRepositoryConstants.FRIEND_REQUEST_STATUS_ACCEPTED_VALUE)
            {
                throw new InvalidOperationException(
                    FriendsRepositoryConstants.ERROR_ALREADY_FRIENDS);
            }

            existingLink.UsuarioIdUsuario1 = participants.RequesterUserId;
            existingLink.UsuarioIdUsuario2 = participants.TargetUserId;
            existingLink.EstadoSolicitud = _statusPendingBinaryValue;

            return existingLink;
        }

        private static FriendLinkDto Map(ListaAmigos entity)
        {
            byte statusValue = GetStatusValue(entity.EstadoSolicitud);

            return new FriendLinkDto
            {
                FriendLinkId = entity.IdListaAmigos,
                UserId1 = entity.UsuarioIdUsuario1,
                UserId2 = entity.UsuarioIdUsuario2,
                Status = (FriendRequestStatus)statusValue
            };
        }

        private static byte GetStatusValue(byte[] statusBinary)
        {
            if (statusBinary == null ||
                statusBinary.Length <
                    FriendsRepositoryConstants.STATUS_MIN_LENGTH)
            {
                return FriendsRepositoryConstants
                    .FRIEND_REQUEST_STATUS_PENDING_VALUE;
            }

            return statusBinary[FriendsRepositoryConstants.STATUS_INDEX];
        }

        private static FriendLinkDto CreateEmptyFriendLink()
        {
            return new FriendLinkDto
            {
                FriendLinkId = FriendsRepositoryConstants.DEFAULT_FRIEND_LINK_ID,
                UserId1 = FriendsRepositoryConstants.DEFAULT_USER_ID,
                UserId2 = FriendsRepositoryConstants.DEFAULT_USER_ID,
                Status = FriendsRepositoryConstants.DEFAULT_FRIEND_REQUEST_STATUS
            };
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                FriendsRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }
    }
}
