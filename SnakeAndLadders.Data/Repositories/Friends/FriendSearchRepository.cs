using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Repositories.Friends
{
    internal sealed class FriendSearchRepository : IFriendSearchRepository
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

        public FriendSearchRepository(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public IReadOnlyList<UserBriefDto> SearchUsers(
            string query,
            int maxResults,
            int excludeUserId)
        {
            ValidateUserId(excludeUserId, nameof(excludeUserId));

            string normalizedQuery = (query ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return new List<UserBriefDto>();
            }

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                ConfigureContext(context);

                int limit = GetEffectiveLimit(maxResults);

                IQueryable<int> pendingTargets = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.UsuarioIdUsuario1 == excludeUserId &&
                        link.EstadoSolicitud == _statusPendingBinaryValue)
                    .Select(link => link.UsuarioIdUsuario2);

                IQueryable<int> acceptedTargets = context.ListaAmigos
                    .AsNoTracking()
                    .Where(link =>
                        link.EstadoSolicitud == _statusAcceptedBinaryValue &&
                        (link.UsuarioIdUsuario1 == excludeUserId ||
                         link.UsuarioIdUsuario2 == excludeUserId))
                    .Select(link =>
                        link.UsuarioIdUsuario1 == excludeUserId
                            ? link.UsuarioIdUsuario2
                            : link.UsuarioIdUsuario1);

                IQueryable<int> blockedIds = pendingTargets
                    .Concat(acceptedTargets)
                    .Distinct();

                IQueryable<UserBriefDto> queryUsers = context.Usuario
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

                return queryUsers.ToList();
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

        private static int GetEffectiveLimit(int maxResults)
        {
            if (maxResults > 0 && maxResults <= FriendsRepositoryConstants.MAX_SEARCH_RESULTS)
            {
                return maxResults;
            }

            return FriendsRepositoryConstants.DEFAULT_SEARCH_MAX_RESULTS;
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                FriendsRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }
    }
}
