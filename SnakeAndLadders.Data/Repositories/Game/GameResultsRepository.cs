using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;
using SnakesAndLadders.Data.Repositories.Game;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace ServerSnakesAndLadders
{
    public sealed class GameResultsRepository : IGameResultsRepository
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(GameResultsRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public GameResultsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public OperationResult<bool> FinalizeGame(
            int gameId,
            int winnerUserId,
            IDictionary<int, int> coinsByUserId)
        {
            if (!IsValidGameId(gameId))
            {
                return OperationResult<bool>.Failure(
                    GameResultsConstants.ERROR_GAME_ID_POSITIVE);
            }

            GameFinalizationRequest request =
                new GameFinalizationRequest(gameId, winnerUserId, coinsByUserId);

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                using (DbContextTransaction transaction =
                    context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    return FinalizeGameInternal(context, transaction, request);
                }
            }
            catch (SqlException ex)
            {
                _logger.Error(GameResultsConstants.LOG_SQL_ERROR_FINALIZING_GAME, ex);
                return OperationResult<bool>.Failure(
                    GameResultsConstants.ERROR_DATABASE_FINALIZING_GAME);
            }
            catch (DbUpdateException ex)
            {
                _logger.Error(
                    GameResultsConstants.LOG_DB_UPDATE_ERROR_FINALIZING_GAME,
                    ex);
                return OperationResult<bool>.Failure(
                    GameResultsConstants.ERROR_DATABASE_UPDATE_FINALIZING_GAME);
            }
            catch (Exception ex)
            {
                _logger.Error(GameResultsConstants.LOG_FATAL_ERROR_FINALIZING_GAME, ex);

                return OperationResult<bool>.Failure(
                    GameResultsConstants.ERROR_FATAL_FINALIZING_GAME);
            }
        }

        private static OperationResult<bool> FinalizeGameInternal(
            SnakeAndLaddersDBEntities1 context,
            DbContextTransaction transaction,
            GameFinalizationRequest request)
        {
            try
            {
                GameResultsHelper.ConfigureContext(context);

                Partida game = FindGameById(context, request.GameId);
                if (game == null)
                {
                    LogGameNotFound(request.GameId);

                    return RollbackWithFailure(
                        transaction,
                        GameResultsConstants.ERROR_GAME_NOT_FOUND);
                }

                MarkGameAsFinished(game);
                UpdateWinnerFlags(context, request.GameId, request.WinnerUserId);

                GameResultsHelper.ApplyCoins(context, request.CoinsByUserId);

                context.SaveChanges();
                transaction.Commit();

                LogFinalizeSuccess(
                    request.GameId,
                    request.WinnerUserId,
                    request.CoinsByUserId);

                return OperationResult<bool>.Success(true);
            }
            catch (SqlException ex)
            {
                _logger.Error(GameResultsConstants.LOG_SQL_ERROR_FINALIZING_GAME, ex);

                return RollbackWithFailure(
                    transaction,
                    GameResultsConstants.ERROR_DATABASE_FINALIZING_GAME);
            }
            catch (DbUpdateException ex)
            {
                _logger.Error(
                    GameResultsConstants.LOG_DB_UPDATE_ERROR_FINALIZING_GAME,
                    ex);

                return RollbackWithFailure(
                    transaction,
                    GameResultsConstants.ERROR_DATABASE_UPDATE_FINALIZING_GAME);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    GameResultsConstants.LOG_UNEXPECTED_ERROR_FINALIZING_GAME,
                    ex);

                return RollbackWithFailure(
                    transaction,
                    GameResultsConstants.ERROR_UNEXPECTED_FINALIZING_GAME);
            }
        }

        private static bool IsValidGameId(int gameId)
        {
            return gameId >= GameResultsConstants.MIN_VALID_GAME_ID;
        }

        private static Partida FindGameById(
            SnakeAndLaddersDBEntities1 context,
            int gameId)
        {
            return context.Partida
                .SingleOrDefault(game => game.IdPartida == gameId);
        }

        private static void MarkGameAsFinished(Partida game)
        {
            game.EstadoPartida = GameResultsConstants.LOBBY_STATUS_CLOSED;
            game.FechaTermino = DateTime.UtcNow;
        }

        private static void UpdateWinnerFlags(
            SnakeAndLaddersDBEntities1 context,
            int gameId,
            int winnerUserId)
        {
            IList<UsuarioHasPartida> links = context.UsuarioHasPartida
                .Where(link => link.PartidaIdPartida == gameId)
                .ToList();

            foreach (UsuarioHasPartida link in links)
            {
                link.Ganador = GameResultsHelper.BuildWinnerFlag(
                    winnerUserId,
                    link.UsuarioIdUsuario);
            }
        }

        private static OperationResult<bool> RollbackWithFailure(
            DbContextTransaction transaction,
            string errorMessage)
        {
            transaction.Rollback();

            return OperationResult<bool>.Failure(errorMessage);
        }

        private static void LogGameNotFound(int gameId)
        {
            _logger.WarnFormat(
                GameResultsConstants.LOG_GAME_NOT_FOUND,
                gameId);
        }

        private static void LogFinalizeSuccess(
            int gameId,
            int winnerUserId,
            IDictionary<int, int> rewardedCoinsByUserId)
        {
            int rewardedUsersCount = rewardedCoinsByUserId.Count;

            _logger.InfoFormat(
                GameResultsConstants.LOG_SUCCESS_FINALIZING_GAME,
                gameId,
                winnerUserId,
                rewardedUsersCount);
        }
    }
}
