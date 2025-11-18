// SnakesAndLadders.Services.Wcf/GameBoardService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GameBoardService : IGameBoardService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardService));

        private const string ERROR_UNEXPECTED_CREATE = "An unexpected internal error occurred while creating the board.";
        private const string ERROR_GAME_ID_INVALID = "GameId must be greater than zero.";

        private readonly GameBoardBuilder gameBoardBuilder = new GameBoardBuilder();
        private readonly IGameSessionStore gameSessionStore;
        private readonly IAppLogger appLogger;

        public GameBoardService(IGameSessionStore gameSessionStore, IAppLogger appLogger)
        {
            this.gameSessionStore = gameSessionStore ?? throw new ArgumentNullException(nameof(gameSessionStore));
            this.appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        public CreateBoardResponseDto CreateBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                const string msg = "Request cannot be null.";
                Logger.Warn(msg);
                throw new FaultException(msg);
            }

            try
            {
                Logger.InfoFormat(
                    "Creating board. GameId={0}, BoardSize={1}, EnableBonusCells={2}, EnableTrapCells={3}, EnableTeleportCells={4}, Difficulty={5}",
                    request.GameId,
                    request.BoardSize,
                    request.EnableBonusCells,
                    request.EnableTrapCells,
                    request.EnableTeleportCells,
                    request.Difficulty);

                var board = gameBoardBuilder.BuildBoard(request);

                var players = (request.PlayerUserIds ?? Array.Empty<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var session = gameSessionStore.CreateSession(request.GameId, board, players);

                Logger.InfoFormat(
                    "Game session created. GameId={0}, Players={1}",
                    session.GameId,
                    string.Join(",", session.PlayerUserIds));

                return new CreateBoardResponseDto
                {
                    Board = board
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn("Validation error while creating board.", ex);
                throw new FaultException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Cannot create game session.", ex);
                throw new FaultException(ex.Message);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ERROR_UNEXPECTED_CREATE, ex);
                throw new FaultException(ERROR_UNEXPECTED_CREATE);
            }
        }

        public BoardDefinitionDto GetBoard(int gameId)
        {
            if (gameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            try
            {
                if (!gameSessionStore.TryGetSession(gameId, out GameSession session))
                {
                    Logger.WarnFormat("GetBoard: no session found for GameId {0}.", gameId);
                    return null;
                }

                return session.Board;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while retrieving board.", ex);
                throw new FaultException("An unexpected error occurred while retrieving the board.");
            }
        }
    }
}
