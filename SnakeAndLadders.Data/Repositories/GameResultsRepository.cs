using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;

namespace ServerSnakesAndLadders
{
    public sealed class GameResultsRepository : IGameResultsRepository
    {
        private const int COMMAND_TIMEOUT_SECONDS = 30;

        private const byte LOBBY_STATUS_CLOSED = (byte)LobbyStatus.Closed;

        private const byte WINNER_FLAG = 0x01;
        private const byte NOT_WINNER_FLAG = 0x00;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameResultsRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> contextFactory;

        public GameResultsRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            this.contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public OperationResult<bool> FinalizeGame(
            int gameId,
            int winnerUserId,
            IDictionary<int, int> coinsByUserId)
        {
            if (gameId <= 0)
            {
                return OperationResult<bool>.Failure("GameId must be positive.");
            }

            try
            {
                using (var context = contextFactory())
                {
                    ConfigureContext(context);

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            Partida partida = context.Partida
                                .SingleOrDefault(p => p.IdPartida == gameId);

                            if (partida == null)
                            {
                                Logger.WarnFormat(
                                    "FinalizeGame: partida not found. GameId={0}",
                                    gameId);

                                transaction.Rollback();
                                return OperationResult<bool>.Failure("Game not found.");
                            }

                            partida.EstadoPartida = LOBBY_STATUS_CLOSED;
                            partida.FechaTermino = DateTime.UtcNow;

                            IList<UsuarioHasPartida> links = context.UsuarioHasPartida
                                .Where(link => link.PartidaIdPartida == gameId)
                                .ToList();

                            foreach (UsuarioHasPartida link in links)
                            {
                                if (winnerUserId > 0 && link.UsuarioIdUsuario == winnerUserId)
                                {
                                    link.Ganador = new[] { WINNER_FLAG };
                                }
                                else
                                {
                                    link.Ganador = new[] { NOT_WINNER_FLAG };
                                }
                            }

                            if (coinsByUserId != null && coinsByUserId.Count > 0)
                            {
                                IList<int> userIds = coinsByUserId.Keys.ToList();

                                IList<Usuario> users = context.Usuario
                                    .Where(u => userIds.Contains(u.IdUsuario))
                                    .ToList();

                                foreach (Usuario user in users)
                                {
                                    if (!coinsByUserId.TryGetValue(user.IdUsuario, out int coinsToAdd))
                                    {
                                        continue;
                                    }

                                    int currentCoins = user.Monedas;
                                    user.Monedas = currentCoins + coinsToAdd;
                                }
                            }

                            context.SaveChanges();
                            transaction.Commit();

                            Logger.InfoFormat(
                                "FinalizeGame: game closed successfully. GameId={0}, WinnerUserId={1}, RewardedUsers={2}",
                                gameId,
                                winnerUserId,
                                coinsByUserId != null ? coinsByUserId.Count : 0);

                            return OperationResult<bool>.Success(true);
                        }
                        catch (SqlException ex)
                        {
                            Logger.Error("SQL error while finalizing game.", ex);
                            transaction.Rollback();

                            return OperationResult<bool>.Failure(
                                "Database error while finalizing game.");
                        }
                        catch (DbUpdateException ex)
                        {
                            Logger.Error("DbUpdate error while finalizing game.", ex);
                            transaction.Rollback();

                            return OperationResult<bool>.Failure(
                                "Database update error while finalizing game.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Unexpected error while finalizing game.", ex);
                            transaction.Rollback();

                            return OperationResult<bool>.Failure(
                                "Unexpected error while finalizing game.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error while creating DB context for FinalizeGame.", ex);

                return OperationResult<bool>.Failure(
                    "Unexpected fatal error while finalizing game.");
            }
        }

        private static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            var objectContext = ((IObjectContextAdapter)context).ObjectContext;
            objectContext.CommandTimeout = COMMAND_TIMEOUT_SECONDS;
        }
    }
}
