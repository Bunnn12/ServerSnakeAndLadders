using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class LobbyRepository : ILobbyRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public LobbyRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public bool CodeExists(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            string normalizedCode = code.Trim();

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                return context.Partida
                    .AsNoTracking()
                    .Any(game => game.CodigoPartida == normalizedCode);
            }
        }

        // <= 3 parámetros: solo 1 (request)
        public CreatedGameInfo CreateGame(CreateLobbyRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request),
                    LobbyRepositoryConstants.ERROR_REQUEST_NULL);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new ArgumentException(
                    LobbyRepositoryConstants.ERROR_CODE_REQUIRED,
                    nameof(request.Code));
            }

            LobbyRepositoryHelper.ValidateUserId(request.HostUserId);

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                {
                    Partida game = CreateGameEntity(request);

                    context.Partida.Add(game);
                    context.SaveChanges();

                    UsuarioHasPartida hostLink = LobbyRepositoryHelper.CreateUserGameLink(
                        game.IdPartida,
                        request.HostUserId,
                        isHost: true);

                    context.UsuarioHasPartida.Add(hostLink);
                    context.SaveChanges();

                    return BuildCreatedGameInfo(game, request);
                }
            }
            catch (DbEntityValidationException ex)
            {
                LogValidationErrors(ex);
                throw;
            }
        }

        public void UpdateGameStatus(int gameId, LobbyStatus newStatus)
        {
            LobbyRepositoryHelper.ValidateGameId(gameId);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                Partida game = context.Partida
                    .SingleOrDefault(partida => partida.IdPartida == gameId);

                if (game == null)
                {
                    return;
                }

                game.EstadoPartida = (byte)newStatus;
                context.SaveChanges();
            }
        }

        public void AddUserToGame(int gameId, int userId, bool isHost)
        {
            LobbyRepositoryHelper.ValidateGameId(gameId);
            LobbyRepositoryHelper.ValidateUserId(userId);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                UsuarioHasPartida link = LobbyRepositoryHelper.CreateUserGameLink(
                    gameId,
                    userId,
                    isHost);

                context.UsuarioHasPartida.Add(link);
                context.SaveChanges();
            }
        }

        public bool IsUserHost(int lobbyId, int userId)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                return context.UsuarioHasPartida
                    .AsNoTracking()
                    .Any(link =>
                        link.PartidaIdPartida == lobbyId &&
                        link.UsuarioIdUsuario == userId &&
                        link.esHost);
            }
        }

        public bool IsUserInLobby(int lobbyId, int userId)
        {
            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                return context.UsuarioHasPartida
                    .AsNoTracking()
                    .Any(link =>
                        link.PartidaIdPartida == lobbyId &&
                        link.UsuarioIdUsuario == userId);
            }
        }

        public void RemoveUserFromLobby(int lobbyId, int userId)
        {
            LobbyRepositoryHelper.ValidateGameId(lobbyId);
            LobbyRepositoryHelper.ValidateUserId(userId);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                UsuarioHasPartida link = context.UsuarioHasPartida
                    .SingleOrDefault(x =>
                        x.PartidaIdPartida == lobbyId &&
                        x.UsuarioIdUsuario == userId);

                if (link == null)
                {
                    return;
                }

                context.UsuarioHasPartida.Remove(link);
                context.SaveChanges();
            }
        }

        // ------------------- privados (≤ 3 parámetros) -------------------

        private static Partida CreateGameEntity(CreateLobbyRequestDto request)
        {
            string safeDifficulty = LobbyRepositoryHelper.NormalizeDifficulty(request.Difficulty);

            return new Partida
            {
                Dificultad = safeDifficulty,
                CodigoPartida = request.Code.Trim(),
                FechaInicio = null,
                FechaTermino = null,
                fechaCreacion = DateTime.UtcNow,
                expiraEn = request.ExpiresAtUtc,
                EstadoPartida = LobbyRepositoryConstants.LOBBY_STATUS_WAITING
            };
        }

        private static CreatedGameInfo BuildCreatedGameInfo(
            Partida game,
            CreateLobbyRequestDto request)
        {
            DateTime effectiveExpiration = game.expiraEn ?? request.ExpiresAtUtc;

            return new CreatedGameInfo
            {
                PartidaId = game.IdPartida,
                Code = game.CodigoPartida,
                ExpiresAtUtc = effectiveExpiration
            };
        }

        private void LogValidationErrors(DbEntityValidationException ex)
        {
            foreach (DbEntityValidationResult entityValidationError in ex.EntityValidationErrors)
            {
                string entityName = entityValidationError.Entry.Entity.GetType().Name;

                foreach (DbValidationError validationError in entityValidationError.ValidationErrors)
                {
                    _logger.ErrorFormat(
                        LobbyRepositoryConstants.LOG_DB_ENTITY_VALIDATION_DETAIL,
                        entityName,
                        validationError.PropertyName,
                        validationError.ErrorMessage);
                }
            }

            _logger.Error(
                LobbyRepositoryConstants.LOG_DB_ENTITY_VALIDATION_ERROR_CREATE_GAME,
                ex);
        }
    }
}
