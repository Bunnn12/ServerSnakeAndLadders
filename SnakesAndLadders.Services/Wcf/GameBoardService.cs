using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Wcf.Constants; // GameBoardBuilder

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GameBoardService : IGameBoardService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardService));

        private readonly GameBoardBuilder _gameBoardBuilder;
        private readonly IGameSessionStore _gameSessionStore;

        public GameBoardService(
            IGameSessionStore gameSessionStore,
            GameBoardBuilder gameBoardBuilder)
        {
            _gameSessionStore = gameSessionStore
                                ?? throw new ArgumentNullException(nameof(gameSessionStore));

            _gameBoardBuilder = gameBoardBuilder
                                ?? throw new ArgumentNullException(nameof(gameBoardBuilder));
        }

        public CreateBoardResponseDto CreateBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                Logger.Warn(GameBoardServiceConstants.ERROR_REQUEST_NULL);
                throw new FaultException(GameBoardServiceConstants.ERROR_REQUEST_NULL);
            }

            if (request.GameId <= 0)
            {
                Logger.Warn(GameBoardServiceConstants.ERROR_GAME_ID_INVALID);
                throw new FaultException(GameBoardServiceConstants.ERROR_GAME_ID_INVALID);
            }

            try
            {
                Logger.InfoFormat(
                    GameBoardServiceConstants.LOG_INFO_CREATING_BOARD_FORMAT,
                    request.GameId,
                    request.BoardSize,
                    request.EnableDiceCells,
                    request.EnableItemCells,
                    request.EnableMessageCells,
                    request.Difficulty);

                BoardDefinitionDto board = _gameBoardBuilder.BuildBoard(request);

                int[] rawPlayerIds = request.PlayerUserIds ?? Array.Empty<int>();

                List<int> players = rawPlayerIds
                    .Where(id => id != GameBoardServiceConstants.INVALID_USER_ID)
                    .Distinct()
                    .ToList();

                if (players.Count == 0)
                {
                    Logger.WarnFormat(
                        GameBoardServiceConstants.LOG_WARN_NO_PLAYERS_FORMAT,
                        request.GameId,
                        rawPlayerIds.Length);

                    throw new InvalidOperationException(GameBoardServiceConstants.ERROR_NO_PLAYERS);
                }

                GameSession session = _gameSessionStore.CreateSession(request.GameId, board, players);

                Logger.InfoFormat(
                    GameBoardServiceConstants.LOG_INFO_SESSION_CREATED_FORMAT,
                    session.GameId,
                    string.Join(",", session.PlayerUserIds));

                return new CreateBoardResponseDto
                {
                    Board = board
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn(GameBoardServiceConstants.LOG_WARN_VALIDATION_CREATE, ex);
                throw new FaultException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(GameBoardServiceConstants.LOG_WARN_CANNOT_CREATE_SESSION, ex);
                throw new FaultException(ex.Message);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(GameBoardServiceConstants.ERROR_UNEXPECTED_CREATE, ex);
                throw new FaultException(GameBoardServiceConstants.ERROR_UNEXPECTED_CREATE);
            }
        }

        public BoardDefinitionDto GetBoard(int gameId)
        {
            if (gameId <= 0)
            {
                Logger.Warn(GameBoardServiceConstants.ERROR_GAME_ID_INVALID);
                throw new FaultException(GameBoardServiceConstants.ERROR_GAME_ID_INVALID);
            }

            try
            {
                if (!_gameSessionStore.TryGetSession(gameId, out GameSession session))
                {
                    Logger.WarnFormat(
                        GameBoardServiceConstants.LOG_WARN_SESSION_NOT_FOUND_FORMAT,
                        gameId);

                    return null;
                }

                return session.Board;
            }
            catch (Exception ex)
            {
                Logger.Error(GameBoardServiceConstants.LOG_ERROR_UNEXPECTED_GET, ex);
                throw new FaultException(GameBoardServiceConstants.ERROR_UNEXPECTED_GET);
            }
        }

    }
}
