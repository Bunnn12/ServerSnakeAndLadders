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
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GameBoardService : IGameBoardService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardService));

        private const string ERROR_UNEXPECTED_CREATE = "An unexpected internal error occurred while creating the board.";
        private const string ERROR_GAME_ID_INVALID = "GameId must be greater than zero.";
        private const string ERROR_REQUEST_NULL = "Request cannot be null.";

        private const int INVALID_USER_ID = 0;

        private readonly GameBoardBuilder gameBoardBuilder = new GameBoardBuilder();
        private readonly IGameSessionStore gameSessionStore;

        public GameBoardService(IGameSessionStore gameSessionStore, IAppLogger appLogger)
        {
            this.gameSessionStore = gameSessionStore ?? throw new ArgumentNullException(nameof(gameSessionStore));
        }

        public CreateBoardResponseDto CreateBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                Logger.Warn(ERROR_REQUEST_NULL);
                throw new FaultException(ERROR_REQUEST_NULL);
            }

            if (request.GameId <= 0)
            {
                Logger.Warn(ERROR_GAME_ID_INVALID);
                throw new FaultException(ERROR_GAME_ID_INVALID);
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

                BoardDefinitionDto board = gameBoardBuilder.BuildBoard(request);

                int[] rawPlayerIds = request.PlayerUserIds ?? Array.Empty<int>();

                List<int> players = rawPlayerIds
                    .Where(id => id != INVALID_USER_ID) // ⬅ aquí aceptas invitados (ids negativos)
                    .Distinct()
                    .ToList();

                if (players.Count == 0)
                {
                    const string message = "Cannot create a game session without players.";
                    Logger.WarnFormat(
                        "CreateBoard: no valid player IDs. GameId={0}, RawCount={1}",
                        request.GameId,
                        rawPlayerIds.Length);

                    throw new InvalidOperationException(message);
                }

                GameSession session = gameSessionStore.CreateSession(request.GameId, board, players);

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
