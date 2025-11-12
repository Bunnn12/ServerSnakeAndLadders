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
            NormalizePair(userIdA, userIdB, out var u1, out var u2);

            using (var ctx = new SnakeAndLaddersDBEntities1())
            {
                var e = ctx.ListaAmigos.AsNoTracking()
                    .SingleOrDefault(x => x.UsuarioIdUsuario1 == u1 && x.UsuarioIdUsuario2 == u2);
                return Map(e);
            }
        }

        public FriendLinkDto CreatePending(int userIdA, int userIdB)
        {
            NormalizePair(userIdA, userIdB, out var u1, out var u2);

            using (var ctx = new SnakeAndLaddersDBEntities1())
            using (var tx = ctx.Database.BeginTransaction())
            {
                var existsUsers = ctx.Usuario
                    .Where(u => u.IdUsuario == u1 || u.IdUsuario == u2)
                    .Select(u => u.IdUsuario)
                    .ToList();

                if (existsUsers.Count != 2)
                {
                    throw new InvalidOperationException();
                }

                var exists = ctx.ListaAmigos
                    .Any(x => x.UsuarioIdUsuario1 == u1 && x.UsuarioIdUsuario2 == u2);
                if (exists)
                {
                    throw new InvalidOperationException();
                }

                var entity = new ListaAmigos
                {
                    UsuarioIdUsuario1 = u1,
                    UsuarioIdUsuario2 = u2,
                    EstadoSolicitud = STATUS_PENDING_BIN
                };

                ctx.ListaAmigos.Add(entity);
                ctx.SaveChanges();
                tx.Commit();

                return Map(entity);
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
                    return; // idempotente
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
    }
}
