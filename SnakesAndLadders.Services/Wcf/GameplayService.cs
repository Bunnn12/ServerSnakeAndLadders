using System;
using System.Collections.Concurrent;
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
    public sealed class GameplayService : IGameplayService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameplayService));

        private const string ERROR_REQUEST_NULL = "Request cannot be null.";
        private const string ERROR_GAME_ID_INVALID = "GameId must be greater than zero.";
        private const string ERROR_USER_ID_INVALID = "UserId must be greater than zero.";
        private const string ERROR_SESSION_NOT_FOUND = "Game session not found for this game.";
        private const string ERROR_UNEXPECTED_ROLL = "Unexpected error while processing dice roll.";
        private const string ERROR_UNEXPECTED_STATE = "Unexpected error while retrieving game state.";

        private readonly IGameSessionStore gameSessionStore;
        private readonly IAppLogger appLogger;

        
        private readonly ConcurrentDictionary<int, GameplayLogic> gameplayByGameId =
            new ConcurrentDictionary<int, GameplayLogic>();

        public GameplayService(IGameSessionStore gameSessionStore, IAppLogger appLogger)
        {
            this.gameSessionStore = gameSessionStore ?? throw new ArgumentNullException(nameof(gameSessionStore));
            this.appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        }

        private GameplayLogic GetOrCreateGameplayLogic(GameSession session)
        {
            return gameplayByGameId.GetOrAdd(
                session.GameId,
                _ => new GameplayLogic(session.Board, session.PlayerUserIds));
        }

        public RollDiceResponseDto RollDice(RollDiceRequestDto request)
        {
            if (request == null)
            {
                throw new FaultException(ERROR_REQUEST_NULL);
            }

            if (request.GameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            if (request.PlayerUserId <= 0)
            {
                throw new FaultException(ERROR_USER_ID_INVALID);
            }

            if (!gameSessionStore.TryGetSession(request.GameId, out GameSession session))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            var logic = GetOrCreateGameplayLogic(session);

            try
            {
                var moveResult = logic.RollDice(request.PlayerUserId);

                
                session.IsFinished = moveResult.IsGameOver;
                gameSessionStore.UpdateSession(session);

                return new RollDiceResponseDto
                {
                    Success = true,
                    DiceValue = moveResult.DiceValue,
                    FromCellIndex = moveResult.FromCellIndex,
                    ToCellIndex = moveResult.ToCellIndex,
                    ExtraInfo = moveResult.ExtraInfo
                };
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Business validation error in RollDice.", ex);
                throw new FaultException(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ERROR_UNEXPECTED_ROLL, ex);
                throw new FaultException(ERROR_UNEXPECTED_ROLL);
            }
        }

        public GetGameStateResponseDto GetGameState(GetGameStateRequestDto request)
        {
            if (request == null)
            {
                throw new FaultException(ERROR_REQUEST_NULL);
            }

            if (request.GameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            if (!gameSessionStore.TryGetSession(request.GameId, out GameSession session))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            try
            {
                var logic = GetOrCreateGameplayLogic(session);
                var state = logic.GetCurrentState();

                return new GetGameStateResponseDto
                {
                    GameId = session.GameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    IsFinished = state.IsFinished,
                    Tokens = state.Tokens
                        .Select(t => new TokenStateDto
                        {
                            UserId = t.UserId,
                            CellIndex = t.CellIndex
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ERROR_UNEXPECTED_STATE, ex);
                throw new FaultException(ERROR_UNEXPECTED_STATE);
            }
        }
    }
}
