using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
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
        private const string ERROR_USER_ID_INVALID = "UserId is invalid.";
        private const string ERROR_SESSION_NOT_FOUND = "Game session not found for this game.";
        private const string ERROR_UNEXPECTED_ROLL = "Unexpected error while processing dice roll.";
        private const string ERROR_UNEXPECTED_STATE = "Unexpected error while retrieving game state.";
        private const string ERROR_JOIN_INVALID = "GameId must be greater than zero and UserId must be non-zero.";
        private const string ERROR_LEAVE_INVALID = "GameId must be greater than zero and UserId must be non-zero.";

        private const string TURN_REASON_NORMAL = "NORMAL";
        private const string TURN_REASON_PLAYER_LEFT = "PLAYER_LEFT";

        private const string LEAVE_REASON_PLAYER_REQUEST = "PLAYER_LEFT";
        private const string LEAVE_REASON_DISCONNECTED = "DISCONNECTED";

        private const string EFFECT_TOKEN_LADDER = "LADDER";
        private const string EFFECT_TOKEN_SNAKE = "SNAKE";

        private const int INVALID_USER_ID = 0;

        private readonly IGameSessionStore gameSessionStore;

        private readonly ConcurrentDictionary<int, GameplayLogic> gameplayByGameId =
            new ConcurrentDictionary<int, GameplayLogic>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>> callbacksByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> userNamesByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, string>>();

        public GameplayService(IGameSessionStore gameSessionStore, IAppLogger appLogger)
        {
            this.gameSessionStore = gameSessionStore ?? throw new ArgumentNullException(nameof(gameSessionStore));
        }

        public RollDiceResponseDto RollDice(RollDiceRequestDto request)
        {
            ValidateRollDiceRequest(request);

            GameSession session = GetSessionOrThrow(request.GameId);
            GameplayLogic logic = GetOrCreateGameplayLogic(session);

            try
            {
                RollDiceResult moveResult = logic.RollDice(request.PlayerUserId);

                session.IsFinished = moveResult.IsGameOver;
                gameSessionStore.UpdateSession(session);

                RollDiceResponseDto rollResponse = BuildRollDiceResponse(request, moveResult);

                BroadcastMoveAndTurn(session, request.PlayerUserId, moveResult);

                return rollResponse;
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
                GameplayLogic logic = GetOrCreateGameplayLogic(session);
                GameStateSnapshot state = logic.GetCurrentState();

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

        public void JoinGame(int gameId, int userId, string userName)
        {
            if (gameId <= 0 || userId == INVALID_USER_ID)
            {
                throw new FaultException(ERROR_JOIN_INVALID);
            }

            if (!gameSessionStore.TryGetSession(gameId, out GameSession _))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            IGameplayCallback callbackChannel = OperationContext.Current.GetCallbackChannel<IGameplayCallback>();

            RegisterCallback(gameId, userId, userName, callbackChannel);

            Logger.InfoFormat(
                "JoinGame: user joined callbacks. GameId={0}, UserId={1}",
                gameId,
                userId);
        }

        public void LeaveGame(int gameId, int userId, string reason)
        {
            if (gameId <= 0 || userId == INVALID_USER_ID)
            {
                throw new FaultException(ERROR_LEAVE_INVALID);
            }

            string safeReason = string.IsNullOrWhiteSpace(reason)
                ? LEAVE_REASON_PLAYER_REQUEST
                : reason;

            RemoveCallback(gameId, userId, safeReason);

            Logger.InfoFormat(
                "LeaveGame: user left callbacks. GameId={0}, UserId={1}, Reason={2}",
                gameId,
                userId,
                safeReason);
        }

        private GameplayLogic GetOrCreateGameplayLogic(GameSession session)
        {
            return gameplayByGameId.GetOrAdd(
                session.GameId,
                gameSessionId => new GameplayLogic(session.Board, session.PlayerUserIds));
        }

        private static void ValidateRollDiceRequest(RollDiceRequestDto request)
        {
            if (request == null)
            {
                throw new FaultException(ERROR_REQUEST_NULL);
            }

            if (request.GameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            if (request.PlayerUserId == INVALID_USER_ID)
            {
                throw new FaultException(ERROR_USER_ID_INVALID);
            }
        }

        private GameSession GetSessionOrThrow(int gameId)
        {
            if (!gameSessionStore.TryGetSession(gameId, out GameSession session))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            return session;
        }

        private static RollDiceResponseDto BuildRollDiceResponse(
            RollDiceRequestDto request,
            RollDiceResult moveResult)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (moveResult == null)
            {
                throw new ArgumentNullException(nameof(moveResult));
            }

            MoveEffectType effectType = MapMoveEffectType(moveResult.ExtraInfo);

            return new RollDiceResponseDto
            {
                Success = true,
                FailureReason = null,
                PlayerUserId = request.PlayerUserId,
                FromCellIndex = moveResult.FromCellIndex,
                ToCellIndex = moveResult.ToCellIndex,
                DiceValue = moveResult.DiceValue,
                MoveResult = effectType,
                ExtraInfo = moveResult.ExtraInfo,
                UpdatedTokens = null
            };
        }

        private void BroadcastMoveAndTurn(
            GameSession session,
            int previousTurnUserId,
            RollDiceResult moveResult)
        {
            if (session == null || moveResult == null)
            {
                return;
            }

            PlayerMoveResultDto moveDto = BuildPlayerMoveResultDto(previousTurnUserId, moveResult);
            NotifyPlayerMoved(session.GameId, moveDto);

            GameStateSnapshot stateAfterMove = GetCurrentStateSafe(session);
            if (stateAfterMove == null)
            {
                return;
            }

            int currentTurnUserId = stateAfterMove.CurrentTurnUserId;
            bool isExtraTurn =
                !moveResult.IsGameOver &&
                previousTurnUserId == currentTurnUserId;

            var turnDto = new TurnChangedDto
            {
                GameId = session.GameId,
                PreviousTurnUserId = previousTurnUserId,
                CurrentTurnUserId = currentTurnUserId,
                IsExtraTurn = isExtraTurn,
                Reason = TURN_REASON_NORMAL
            };

            NotifyTurnChanged(turnDto);
        }

        private static PlayerMoveResultDto BuildPlayerMoveResultDto(
            int userId,
            RollDiceResult moveResult)
        {
            if (moveResult == null)
            {
                throw new ArgumentNullException(nameof(moveResult));
            }

            MoveEffectType effectType = MapMoveEffectType(moveResult.ExtraInfo);

            return new PlayerMoveResultDto
            {
                UserId = userId,
                FromCellIndex = moveResult.FromCellIndex,
                ToCellIndex = moveResult.ToCellIndex,
                DiceValue = moveResult.DiceValue,
                HasExtraTurn = false,
                HasWon = moveResult.IsGameOver,
                Message = moveResult.ExtraInfo,
                EffectType = effectType
            };
        }

        private static MoveEffectType MapMoveEffectType(string extraInfo)
        {
            if (string.IsNullOrWhiteSpace(extraInfo))
            {
                return MoveEffectType.None;
            }

            string normalized = extraInfo.ToUpperInvariant();

            if (normalized.Contains(EFFECT_TOKEN_LADDER))
            {
                return MoveEffectType.Ladder;
            }

            if (normalized.Contains(EFFECT_TOKEN_SNAKE))
            {
                return MoveEffectType.Snake;
            }

            return MoveEffectType.None;
        }

        private GameStateSnapshot GetCurrentStateSafe(GameSession session)
        {
            try
            {
                GameplayLogic logic = GetOrCreateGameplayLogic(session);
                return logic.GetCurrentState();
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to retrieve current state after move.", ex);
                return null;
            }
        }

        private void RegisterCallback(
            int gameId,
            int userId,
            string userName,
            IGameplayCallback callbackChannel)
        {
            var callbacksForGame = callbacksByGameId.GetOrAdd(
                gameId,
                callbacksDictionaryGameId => new ConcurrentDictionary<int, IGameplayCallback>());

            callbacksForGame[userId] = callbackChannel;

            var userNamesForGame = userNamesByGameId.GetOrAdd(
                gameId,
                userNamesDictionaryGameId => new ConcurrentDictionary<int, string>());

            string effectiveUserName = string.IsNullOrWhiteSpace(userName)
                ? $"User {userId}"
                : userName;

            userNamesForGame[userId] = effectiveUserName;
        }

        private void RemoveCallback(int gameId, int userId, string leaveReason)
        {
            if (!callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return;
            }

            bool wasRemoved = callbacksForGame.TryRemove(userId, out IGameplayCallback _);

            if (!wasRemoved)
            {
                return;
            }

            string userName = GetUserNameOrDefault(gameId, userId);

            bool wasCurrentTurn = false;
            int? newCurrentTurnUserId = null;

            try
            {
                if (gameSessionStore.TryGetSession(gameId, out GameSession session))
                {
                    GameStateSnapshot state = GetCurrentStateSafe(session);
                    if (state != null)
                    {
                        wasCurrentTurn = state.CurrentTurnUserId == userId;
                        newCurrentTurnUserId = wasCurrentTurn
                            ? (int?)FindNextPlayerId(session, userId)
                            : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Error computing new turn after player left.", ex);
            }

            var leftDto = new PlayerLeftDto
            {
                GameId = gameId,
                UserId = userId,
                UserName = userName,
                WasCurrentTurnPlayer = wasCurrentTurn,
                NewCurrentTurnUserId = newCurrentTurnUserId,
                Reason = leaveReason
            };

            NotifyPlayerLeft(leftDto);

            if (wasCurrentTurn && newCurrentTurnUserId.HasValue)
            {
                var turnDto = new TurnChangedDto
                {
                    GameId = gameId,
                    PreviousTurnUserId = userId,
                    CurrentTurnUserId = newCurrentTurnUserId.Value,
                    IsExtraTurn = false,
                    Reason = TURN_REASON_PLAYER_LEFT
                };

                NotifyTurnChanged(turnDto);
            }

            CleanupDictionariesIfEmpty(gameId, callbacksForGame);
        }

        private static int FindNextPlayerId(GameSession session, int leavingUserId)
        {
            var orderedPlayers = session.PlayerUserIds
                .Where(id => id != INVALID_USER_ID)
                .OrderBy(id => id)
                .ToList();

            if (!orderedPlayers.Any())
            {
                return INVALID_USER_ID;
            }

            int index = orderedPlayers.IndexOf(leavingUserId);
            if (index < 0)
            {
                return orderedPlayers[0];
            }

            int nextIndex = (index + 1) % orderedPlayers.Count;
            return orderedPlayers[nextIndex];
        }

        private void CleanupDictionariesIfEmpty(
            int gameId,
            ConcurrentDictionary<int, IGameplayCallback> callbacksForGame)
        {
            if (!callbacksForGame.IsEmpty)
            {
                return;
            }

            callbacksByGameId.TryRemove(gameId, out _);
            userNamesByGameId.TryRemove(gameId, out _);
        }

        private string GetUserNameOrDefault(int gameId, int userId)
        {
            if (!userNamesByGameId.TryGetValue(gameId, out var usersForGame))
            {
                return $"User {userId}";
            }

            if (!usersForGame.TryGetValue(userId, out string userName))
            {
                return $"User {userId}";
            }

            return userName;
        }

        private void NotifyPlayerMoved(int gameId, PlayerMoveResultDto move)
        {
            if (!callbacksByGameId.TryGetValue(gameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    gameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnPlayerMoved(move));
            }
        }

        private void NotifyTurnChanged(TurnChangedDto turnInfo)
        {
            if (!callbacksByGameId.TryGetValue(turnInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    turnInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnTurnChanged(turnInfo));
            }
        }

        private void NotifyPlayerLeft(PlayerLeftDto playerLeftInfo)
        {
            if (!callbacksByGameId.TryGetValue(playerLeftInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    playerLeftInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnPlayerLeft(playerLeftInfo));
            }
        }

        private void InvokeCallbackSafely(
            int gameId,
            int userId,
            IGameplayCallback callback,
            Action<IGameplayCallback> callbackInvoker)
        {
            try
            {
                callbackInvoker(callback);
            }
            catch (Exception ex)
            {
                Logger.WarnFormat(
                    "Callback invocation failed. GameId={0}, UserId={1}. Removing callback. Exception={2}",
                    gameId,
                    userId,
                    ex);

                RemoveCallback(gameId, userId, LEAVE_REASON_DISCONNECTED);
            }
        }
    }
}
