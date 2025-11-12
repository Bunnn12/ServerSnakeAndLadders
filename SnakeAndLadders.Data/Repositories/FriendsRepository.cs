using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class FriendsRepository : IFriendsRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsRepository));

        private static readonly byte[] STATUS_PENDING_BIN = { 0x01 };
        private static readonly byte[] STATUS_ACCEPTED_BIN = { 0x02 };

        public FriendLinkDto GetById(int friendLinkId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var e = ctx.ListaAmigos.AsNoTracking()
                    .SingleOrDefault(x => x.IdListaAmigos == friendLinkId);
                return Map(e);
            }
        }

        public FriendLinkDto GetNormalized(int userIdA, int userIdB)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var e = ctx.ListaAmigos.AsNoTracking()
                    .SingleOrDefault(x =>
                        (x.UsuarioIdUsuario1 == userIdA && x.UsuarioIdUsuario2 == userIdB) ||
                        (x.UsuarioIdUsuario1 == userIdB && x.UsuarioIdUsuario2 == userIdA));
                return Map(e);
            }
        }

        public FriendLinkDto CreatePending(int requesterUserId, int targetUserId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            using (var tx = ctx.Database.BeginTransaction())
            {
                var existsUsers = ctx.Usuario
                    .Where(u => u.IdUsuario == requesterUserId || u.IdUsuario == targetUserId)
                    .Select(u => u.IdUsuario)
                    .ToList();
                if (existsUsers.Count != 2)
                {
                    throw new InvalidOperationException("Users not found.");
                }

                var link = ctx.ListaAmigos
                    .SingleOrDefault(x =>
                        (x.UsuarioIdUsuario1 == requesterUserId && x.UsuarioIdUsuario2 == targetUserId) ||
                        (x.UsuarioIdUsuario1 == targetUserId && x.UsuarioIdUsuario2 == requesterUserId));

                if (link == null)
                {
                    var entity = new ListaAmigos
                    {
                        UsuarioIdUsuario1 = requesterUserId,
                        UsuarioIdUsuario2 = targetUserId,
                        EstadoSolicitud = STATUS_PENDING_BIN
                    };
                    ctx.ListaAmigos.Add(entity);
                    ctx.SaveChanges();
                    tx.Commit();
                    return Map(entity);
                }

                var currentStatus = (link.EstadoSolicitud != null && link.EstadoSolicitud.Length > 0)
                    ? link.EstadoSolicitud[0]
                    : (byte)0x01; 

                if (currentStatus == STATUS_PENDING_BIN[0])
                {
                    bool isReverse =
                        link.UsuarioIdUsuario1 == targetUserId &&
                        link.UsuarioIdUsuario2 == requesterUserId;

                    if (isReverse)
                    {
                        link.EstadoSolicitud = STATUS_ACCEPTED_BIN;
                        ctx.SaveChanges();
                        tx.Commit();
                        return Map(link);
                    }

                    throw new InvalidOperationException("Pending already exists.");
                }

                if (currentStatus == STATUS_ACCEPTED_BIN[0])
                {
                    throw new InvalidOperationException("Already friends.");
                }
                link.UsuarioIdUsuario1 = requesterUserId;
                link.UsuarioIdUsuario2 = targetUserId;
                link.EstadoSolicitud = STATUS_PENDING_BIN;

                ctx.SaveChanges();
                tx.Commit();
                return Map(link);
            }
        }



        public void UpdateStatus(int friendLinkId, byte newStatus)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            using (var tx = ctx.Database.BeginTransaction())
            {
                var entity = ctx.ListaAmigos.SingleOrDefault(x => x.IdListaAmigos == friendLinkId);
                if (entity == null)
                {
                    throw new InvalidOperationException();
                }

                entity.EstadoSolicitud = new[] { newStatus }; 
                ctx.SaveChanges();
                tx.Commit();
            }
        }

        public void DeleteLink(int friendLinkId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            using (var tx = ctx.Database.BeginTransaction())
            {
                var entity = ctx.ListaAmigos.SingleOrDefault(x => x.IdListaAmigos == friendLinkId);
                if (entity == null)
                {
                    return; 
                }

                ctx.ListaAmigos.Remove(entity);
                ctx.SaveChanges();
                tx.Commit();
            }
        }

        public IReadOnlyList<int> GetAcceptedFriendsIds(int userId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var q = ctx.ListaAmigos.AsNoTracking()
                    .Where(x => x.EstadoSolicitud == STATUS_ACCEPTED_BIN &&
                               (x.UsuarioIdUsuario1 == userId || x.UsuarioIdUsuario2 == userId))
                    .Select(x => x.UsuarioIdUsuario1 == userId ? x.UsuarioIdUsuario2 : x.UsuarioIdUsuario1);

                return q.ToList();
            }
        }

        public IReadOnlyList<FriendLinkDto> GetPendingRelated(int userId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var q = ctx.ListaAmigos.AsNoTracking()
                    .Where(x => x.EstadoSolicitud == STATUS_PENDING_BIN &&
                               (x.UsuarioIdUsuario1 == userId || x.UsuarioIdUsuario2 == userId));

                return q.ToList().Select(Map).ToList();
            }
        }

        private static void NormalizePair(int a, int b, out int min, out int max)
        {
            if (a < b) { min = a; max = b; }
            else { min = b; max = a; }
        }

        private static FriendLinkDto Map(ListaAmigos e)
        {
            if (e == null) return null;

            var status = (e.EstadoSolicitud != null && e.EstadoSolicitud.Length > 0)
                ? e.EstadoSolicitud[0]
                : (byte)0x01;

            return new FriendLinkDto
            {
                FriendLinkId = e.IdListaAmigos,
                UserId1 = e.UsuarioIdUsuario1,
                UserId2 = e.UsuarioIdUsuario2,
                Status = (FriendRequestStatus)status
            };
        }
        public IReadOnlyList<FriendListItemDto> GetAcceptedFriendsDetailed(int userId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var rows = ctx.ListaAmigos.AsNoTracking()
                    .Where(x => x.EstadoSolicitud == STATUS_ACCEPTED_BIN &&
                                (x.UsuarioIdUsuario1 == userId || x.UsuarioIdUsuario2 == userId))
                    .Select(x => new
                    {
                        x.IdListaAmigos,
                        FriendUserId = x.UsuarioIdUsuario1 == userId ? x.UsuarioIdUsuario2 : x.UsuarioIdUsuario1
                    })
                    .ToList();

                var friendIds = rows.Select(r => r.FriendUserId).ToList();

                var users = ctx.Usuario.AsNoTracking()
                    .Where(u => friendIds.Contains(u.IdUsuario))
                    .Select(u => new { u.IdUsuario, u.NombreUsuario, u.FotoPerfil })
                    .ToList()
                    .ToDictionary(u => u.IdUsuario);

                return rows.Select(r => new FriendListItemDto
                {
                    FriendLinkId = r.IdListaAmigos,
                    FriendUserId = r.FriendUserId,
                    FriendUserName = users.TryGetValue(r.FriendUserId, out var u) ? u.NombreUsuario : string.Empty,
                    ProfilePhotoId = users.TryGetValue(r.FriendUserId, out var u2) ? u2.FotoPerfil : null
                }).ToList();
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetIncomingPendingDetailed(int userId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var pend = ctx.ListaAmigos.AsNoTracking()
                    .Where(x => x.EstadoSolicitud == STATUS_PENDING_BIN &&
                                (x.UsuarioIdUsuario1 == userId || x.UsuarioIdUsuario2 == userId))
                    .ToList();

                var incoming = pend.Where(x => x.UsuarioIdUsuario2 == userId).ToList(); // el que “recibe” es userId2

                var involvedIds = incoming.Select(x => x.UsuarioIdUsuario1).Concat(incoming.Select(x => x.UsuarioIdUsuario2)).Distinct().ToList();
                var users = ctx.Usuario.AsNoTracking()
                    .Where(u => involvedIds.Contains(u.IdUsuario))
                    .Select(u => new { u.IdUsuario, u.NombreUsuario })
                    .ToList()
                    .ToDictionary(u => u.IdUsuario);

                return incoming.Select(e => new FriendRequestItemDto
                {
                    FriendLinkId = e.IdListaAmigos,
                    RequesterUserId = e.UsuarioIdUsuario1,
                    RequesterUserName = users[e.UsuarioIdUsuario1].NombreUsuario,
                    TargetUserId = e.UsuarioIdUsuario2,
                    TargetUserName = users[e.UsuarioIdUsuario2].NombreUsuario,
                    Status = FriendRequestStatus.Pending
                }).ToList();
            }
        }

        public IReadOnlyList<FriendRequestItemDto> GetOutgoingPendingDetailed(int userId)
        {
            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var outgoing = ctx.ListaAmigos.AsNoTracking()
                    .Where(x => x.EstadoSolicitud == STATUS_PENDING_BIN &&
                                x.UsuarioIdUsuario1 == userId)
                    .ToList();

                var involvedIds = outgoing.Select(x => x.UsuarioIdUsuario1).Concat(outgoing.Select(x => x.UsuarioIdUsuario2)).Distinct().ToList();
                var users = ctx.Usuario.AsNoTracking()
                    .Where(u => involvedIds.Contains(u.IdUsuario))
                    .Select(u => new { u.IdUsuario, u.NombreUsuario })
                    .ToList()
                    .ToDictionary(u => u.IdUsuario);

                return outgoing.Select(e => new FriendRequestItemDto
                {
                    FriendLinkId = e.IdListaAmigos,
                    RequesterUserId = e.UsuarioIdUsuario1,
                    RequesterUserName = users[e.UsuarioIdUsuario1].NombreUsuario,
                    TargetUserId = e.UsuarioIdUsuario2,
                    TargetUserName = users[e.UsuarioIdUsuario2].NombreUsuario,
                    Status = FriendRequestStatus.Pending
                }).ToList();
            }
        }

        public IReadOnlyList<UserBriefDto> SearchUsers(string query, int maxResults, int excludeUserId)
        {
            query = (query ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<UserBriefDto>();
            }

            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                const int DEFAULT_MAX = 20;
                int limit = maxResults > 0 && maxResults <= 100 ? maxResults : DEFAULT_MAX;

                var pendingTargets = ctx.ListaAmigos.AsNoTracking()
                    .Where(fr => fr.UsuarioIdUsuario1 == excludeUserId && fr.EstadoSolicitud == STATUS_PENDING_BIN)
                    .Select(fr => fr.UsuarioIdUsuario2);

                var acceptedTargets = ctx.ListaAmigos.AsNoTracking()
                    .Where(fr =>
                        fr.EstadoSolicitud == STATUS_ACCEPTED_BIN &&
                       (fr.UsuarioIdUsuario1 == excludeUserId || fr.UsuarioIdUsuario2 == excludeUserId))
                    .Select(fr => fr.UsuarioIdUsuario1 == excludeUserId ? fr.UsuarioIdUsuario2 : fr.UsuarioIdUsuario1);

                var blockedIds = pendingTargets
                    .Concat(acceptedTargets) 
                    .Distinct();

                var q = ctx.Usuario.AsNoTracking()
                    .Where(u => u.IdUsuario != excludeUserId &&
                                u.NombreUsuario.Contains(query) &&
                                !blockedIds.Contains(u.IdUsuario))
                    .OrderBy(u => u.NombreUsuario)
                    .Take(limit)
                    .Select(u => new UserBriefDto
                    {
                        UserId = u.IdUsuario,
                        UserName = u.NombreUsuario,
                        ProfilePhotoId = u.FotoPerfil
                    });

                return q.ToList();
            }
        }

    }
}
