using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Gameplay;
using SnakesAndLadders.Services.Wcf.Gameplay;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Timers;

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
        private const string ERROR_USE_ITEM_INVALID_SLOT = "Item slot number is invalid.";
        private const string ERROR_USE_ITEM_NO_ITEM_IN_SLOT = "No item is equipped in the selected slot.";
        private const string ERROR_USE_ITEM_NO_QUANTITY = "User does not have any remaining units of the selected item.";

        private const string ERROR_USE_DICE_INVALID_SLOT = "Dice slot number is invalid.";
        private const string ERROR_USE_DICE_NO_DICE_IN_SLOT = "No dice is equipped in the selected slot.";
        private const string ERROR_USE_DICE_NO_QUANTITY = "User does not have any remaining units of the selected dice.";

        private const string ERROR_GRANT_REWARD_FAILED = "Error while granting special cell reward.";

        private const string ERROR_INVENTORY_UNAVAILABLE =
            "No se pudo acceder al inventario. Intenta de nuevo más tarde.";

        private const string ERROR_DICE_INVENTORY_UNAVAILABLE =
            "No se pudo acceder a los dados equipados. Intenta de nuevo más tarde.";

        private const string TURN_REASON_NORMAL = "NORMAL";
        private const string TURN_REASON_PLAYER_LEFT = "PLAYER_LEFT";

        private const string LEAVE_REASON_PLAYER_REQUEST = "PLAYER_LEFT";
        private const string LEAVE_REASON_DISCONNECTED = "DISCONNECTED";

        private const string EFFECT_TOKEN_ROCKET_USED = "ROCKET_USED";
        private const string EFFECT_TOKEN_ROCKET_IGNORED = "ROCKET_IGNORED";

        private const string TURN_REASON_TIMEOUT_SKIP = "TIMEOUT_SKIP";
        private const string TURN_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";
        private const string LEAVE_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";

        private const string END_REASON_BOARD_WIN = "BOARD_WIN";
        private const string END_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";
        private const string END_REASON_LAST_PLAYER_REMAINING = "LAST_PLAYER_REMAINING";

        private const int INVALID_USER_ID = 0;

        private const byte MIN_ITEM_SLOT = 1;
        private const byte MAX_ITEM_SLOT = 3;

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private const int TURN_TIME_SECONDS = 30;

        private const int COINS_FIRST_PLACE = 50;
        private const int COINS_SECOND_PLACE = 30;
        private const int COINS_THIRD_PLACE = 10;

        private readonly IGameSessionStore gameSessionStore;
        private readonly IInventoryRepository inventoryRepository;
        private readonly IGameResultsRepository gameResultsRepository;

        private readonly GameplayInventoryService inventoryService;

        private readonly ConcurrentDictionary<int, GameplayLogic> gameplayByGameId =
            new ConcurrentDictionary<int, GameplayLogic>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>> callbacksByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, IGameplayCallback>>();

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> userNamesByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, string>>();

        private readonly ConcurrentDictionary<int, TurnTimerState> turnTimersByGameId =
            new ConcurrentDictionary<int, TurnTimerState>();

        private readonly ConcurrentDictionary<int, Timer> timersByGameId =
            new ConcurrentDictionary<int, Timer>();

        public GameplayService(
            IGameSessionStore gameSessionStore,
            IInventoryRepository inventoryRepository,
            IGameResultsRepository gameResultsRepository,
            IAppLogger appLogger)
        {
            this.gameSessionStore = gameSessionStore
                ?? throw new ArgumentNullException(nameof(gameSessionStore));

            this.inventoryRepository = inventoryRepository
                ?? throw new ArgumentNullException(nameof(inventoryRepository));

            this.gameResultsRepository = gameResultsRepository
                ?? throw new ArgumentNullException(nameof(gameResultsRepository));

            inventoryService = new GameplayInventoryService(
                this.inventoryRepository,
                Logger);
        }

        public RollDiceResponseDto RollDice(RollDiceRequestDto request)
        {
            ValidateRollDiceRequest(request);

            GameSession session = GetSessionOrThrow(request.GameId);
            GameplayLogic logic = GetOrCreateGameplayLogic(session);

            InventoryDiceDto equippedDice = null;
            string diceCode = null;
            int? diceIdToConsume = null;

            if (request.DiceSlotNumber.HasValue)
            {
                equippedDice = inventoryService.ResolveEquippedDiceForSlot(
                    request.PlayerUserId,
                    request.DiceSlotNumber.Value,
                    ERROR_USE_DICE_NO_DICE_IN_SLOT,
                    ERROR_USE_DICE_NO_QUANTITY,
                    ERROR_DICE_INVENTORY_UNAVAILABLE);

                diceCode = equippedDice.DiceCode;
                diceIdToConsume = equippedDice.DiceId;
            }

            try
            {
                RollDiceResult moveResult = logic.RollDice(
                    request.PlayerUserId,
                    diceCode);

                inventoryService.GrantRewardsFromSpecialCells(
                    request.PlayerUserId,
                    moveResult,
                    ERROR_GRANT_REWARD_FAILED);

                if (diceIdToConsume.HasValue && !string.IsNullOrWhiteSpace(diceCode))
                {
                    try
                    {
                        inventoryRepository.ConsumeDice(
                            request.PlayerUserId,
                            diceIdToConsume.Value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error while consuming dice after roll.", ex);
                    }
                }

                inventoryService.HandlePendingRocketConsumption(
                    session.GameId,
                    request.PlayerUserId,
                    moveResult,
                    EFFECT_TOKEN_ROCKET_USED,
                    EFFECT_TOKEN_ROCKET_IGNORED);

                session.IsFinished = moveResult.IsGameOver;

                if (moveResult.IsGameOver)
                {
                    session.HasWinner = true;
                    session.WinnerUserId = request.PlayerUserId;
                    session.WinnerUserName = GetUserNameOrDefault(session.GameId, request.PlayerUserId);
                    session.EndReason = END_REASON_BOARD_WIN;
                    session.FinishedAtUtc = DateTime.UtcNow;
                    session.CurrentTurnStartUtc = DateTime.MinValue;

                    StopTurnTimer(session.GameId);

                    gameSessionStore.UpdateSession(session);
                    FinalizeGame(session);
                }
                else
                {
                    gameSessionStore.UpdateSession(session);
                }

                RollDiceResponseDto rollResponse = GameplayResponseBuilder.BuildRollDiceResponse(
                    request,
                    moveResult);

                BroadcastMoveAndTurn(
                    session,
                    request.PlayerUserId,
                    moveResult);

                return rollResponse;
            }
            catch (FaultException)
            {
                throw;
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

        internal void ProcessTurnTimeout(int gameId)
        {
            GameSession session;

            try
            {
                session = GetSessionOrThrow(gameId);
            }
            catch (FaultException)
            {
                return;
            }

            GameplayLogic logic = GetOrCreateGameplayLogic(session);

            TurnTimeoutResult result;

            try
            {
                result = logic.HandleTurnTimeout();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Business validation error while processing turn timeout.", ex);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while processing turn timeout.", ex);
                return;
            }

            if (result.PlayerKicked && result.KickedUserId != INVALID_USER_ID)
            {
                var updatedPlayers = session.PlayerUserIds
                    .Where(id => id != result.KickedUserId)
                    .ToList();

                session.PlayerUserIds = updatedPlayers;
            }

            session.IsFinished = result.GameFinished;

            if (result.WinnerUserId != INVALID_USER_ID)
            {
                session.HasWinner = true;
                session.WinnerUserId = result.WinnerUserId;
                session.WinnerUserName = GetUserNameOrDefault(gameId, result.WinnerUserId);
                session.EndReason = END_REASON_TIMEOUT_KICK;
            }

            if (session.IsFinished)
            {
                if (session.FinishedAtUtc == DateTime.MinValue)
                {
                    session.FinishedAtUtc = DateTime.UtcNow;
                }

                session.CurrentTurnStartUtc = DateTime.MinValue;
            }
            else if (result.CurrentTurnUserId != INVALID_USER_ID)
            {
                session.CurrentTurnUserId = result.CurrentTurnUserId;
                session.CurrentTurnStartUtc = DateTime.UtcNow;
            }
            else
            {
                session.CurrentTurnStartUtc = DateTime.MinValue;
            }

            gameSessionStore.UpdateSession(session);

            if (session.IsFinished)
            {
                FinalizeGame(session);
            }

            if (result.PlayerKicked && result.KickedUserId != INVALID_USER_ID)
            {
                string userName = GetUserNameOrDefault(gameId, result.KickedUserId);

                var leftDto = new PlayerLeftDto
                {
                    GameId = gameId,
                    UserId = result.KickedUserId,
                    UserName = userName,
                    WasCurrentTurnPlayer = true,
                    NewCurrentTurnUserId = session.IsFinished
                        ? (int?)null
                        : (int?)result.CurrentTurnUserId,
                    Reason = LEAVE_REASON_TIMEOUT_KICK
                };

                NotifyPlayerLeft(leftDto);
            }

            if (!session.IsFinished && result.CurrentTurnUserId != INVALID_USER_ID)
            {
                var turnDto = new TurnChangedDto
                {
                    GameId = gameId,
                    PreviousTurnUserId = result.PreviousTurnUserId,
                    CurrentTurnUserId = result.CurrentTurnUserId,
                    IsExtraTurn = false,
                    Reason = result.PlayerKicked
                        ? TURN_REASON_TIMEOUT_KICK
                        : TURN_REASON_TIMEOUT_SKIP
                };

                NotifyTurnChanged(turnDto);

                StartOrResetTurnTimer(session);
            }
            else
            {
                StopTurnTimer(gameId);
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

                int remainingSeconds = 0;

                if (!session.IsFinished && state.CurrentTurnUserId != INVALID_USER_ID)
                {
                    if (turnTimersByGameId.TryGetValue(session.GameId, out TurnTimerState timerState) &&
                        timerState.CurrentTurnUserId == state.CurrentTurnUserId)
                    {
                        remainingSeconds = timerState.RemainingSeconds;
                    }
                    else
                    {
                        StartOrResetTurnTimer(session);
                        remainingSeconds = TURN_TIME_SECONDS;
                    }
                }

                bool isFinished = state.IsFinished || session.IsFinished;

                return new GetGameStateResponseDto
                {
                    GameId = session.GameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    IsFinished = isFinished,
                    WinnerUserId = session.WinnerUserId,
                    Tokens = state.Tokens.ToList(),
                    RemainingTurnSeconds = remainingSeconds
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ERROR_UNEXPECTED_STATE, ex);
                throw new FaultException(ERROR_UNEXPECTED_STATE);
            }
        }

        public UseItemResponseDto UseItem(UseItemRequestDto request)
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

            if (request.ItemSlotNumber < MIN_ITEM_SLOT || request.ItemSlotNumber > MAX_ITEM_SLOT)
            {
                throw new FaultException(ERROR_USE_ITEM_INVALID_SLOT);
            }

            GameSession session = GetSessionOrThrow(request.GameId);
            GameplayLogic logic = GetOrCreateGameplayLogic(session);

            InventoryItemDto equippedItem;

            try
            {
                equippedItem = inventoryService.ResolveEquippedItemForSlot(
                    request.PlayerUserId,
                    request.ItemSlotNumber,
                    ERROR_USE_ITEM_NO_ITEM_IN_SLOT,
                    ERROR_USE_ITEM_NO_QUANTITY,
                    ERROR_INVENTORY_UNAVAILABLE);
            }
            catch (FaultException)
            {
                throw;
            }

            try
            {
                ItemEffectResult effectResult = logic.UseItem(
                    request.PlayerUserId,
                    equippedItem.ObjectCode,
                    request.TargetUserId);

                bool isRocket = effectResult.EffectType == ItemEffectType.Rocket;
                bool shouldConsume = effectResult.ShouldConsumeItemImmediately;

                if (effectResult.WasBlockedByShield)
                {
                    shouldConsume = false;
                }

                if (effectResult.EffectType == ItemEffectType.Anchor &&
                    effectResult.FromCellIndex == effectResult.ToCellIndex)
                {
                    shouldConsume = false;
                }

                if (isRocket && !effectResult.WasBlockedByShield)
                {
                    inventoryService.TrackPendingRocket(
                        session.GameId,
                        request.PlayerUserId,
                        request.ItemSlotNumber,
                        equippedItem.ObjectId,
                        equippedItem.ObjectCode);
                }

                if (shouldConsume)
                {
                    try
                    {
                        inventoryRepository.ConsumeItem(
                            request.PlayerUserId,
                            equippedItem.ObjectId);

                        inventoryRepository.RemoveItemFromSlot(
                            request.PlayerUserId,
                            request.ItemSlotNumber);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Unexpected error while consuming item after usage.", ex);
                    }
                }

                GetGameStateResponseDto updatedGameState = GetGameState(
                    new GetGameStateRequestDto
                    {
                        GameId = request.GameId
                    });

                var response = new UseItemResponseDto
                {
                    Success = true,
                    FailureReason = null,
                    GameId = request.GameId,
                    PlayerUserId = request.PlayerUserId,
                    TargetUserId = request.TargetUserId,
                    ItemCode = equippedItem.ObjectCode,
                    EffectType = effectResult.EffectType,
                    UpdatedGameState = updatedGameState
                };

                var notification = new ItemUsedNotificationDto
                {
                    GameId = request.GameId,
                    UserId = request.PlayerUserId,
                    TargetUserId = request.TargetUserId,
                    ItemCode = equippedItem.ObjectCode,
                    EffectResult = new ItemEffectResultDto
                    {
                        ItemCode = effectResult.ItemCode,
                        EffectType = effectResult.EffectType,
                        UserId = effectResult.UserId,
                        TargetUserId = effectResult.TargetUserId,
                        FromCellIndex = effectResult.FromCellIndex,
                        ToCellIndex = effectResult.ToCellIndex,
                        WasBlockedByShield = effectResult.WasBlockedByShield,
                        TargetFrozen = effectResult.TargetFrozen,
                        ShieldActivated = effectResult.ShieldActivated
                    },
                    UpdatedGameState = updatedGameState
                };

                NotifyItemUsed(request.GameId, notification);

                return response;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Business validation error in UseItem.", ex);

                return new UseItemResponseDto
                {
                    Success = false,
                    FailureReason = ex.Message,
                    GameId = request.GameId,
                    PlayerUserId = request.PlayerUserId,
                    TargetUserId = request.TargetUserId,
                    ItemCode = equippedItem.ObjectCode,
                    EffectType = ItemEffectType.None,
                    UpdatedGameState = null
                };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while processing item usage.", ex);
                throw new FaultException("Unexpected error while processing item usage.");
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

            IGameplayCallback callbackChannel = OperationContext.Current
                .GetCallbackChannel<IGameplayCallback>();

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
                _ => new GameplayLogic(session.Board, session.PlayerUserIds));
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

            if (request.DiceSlotNumber.HasValue)
            {
                byte slot = request.DiceSlotNumber.Value;

                if (slot < MIN_DICE_SLOT || slot > MAX_DICE_SLOT)
                {
                    throw new FaultException(ERROR_USE_DICE_INVALID_SLOT);
                }
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

        private static int GetCoinsForPosition(int position)
        {
            switch (position)
            {
                case 1:
                    return COINS_FIRST_PLACE;
                case 2:
                    return COINS_SECOND_PLACE;
                case 3:
                    return COINS_THIRD_PLACE;
                default:
                    return 0;
            }
        }

        private IDictionary<int, int> BuildCoinsDistribution(GameSession session)
        {
            var coinsByUserId = new Dictionary<int, int>();

            if (session == null)
            {
                return coinsByUserId;
            }

            GameStateSnapshot state = GetCurrentStateSafe(session);
            if (state == null || state.Tokens == null || state.Tokens.Count == 0)
            {
                return coinsByUserId;
            }

            var activeUserIds = new HashSet<int>();

            if (callbacksByGameId.TryGetValue(session.GameId, out var callbacksForGame))
            {
                foreach (var entry in callbacksForGame)
                {
                    activeUserIds.Add(entry.Key);
                }
            }

            List<TokenStateDto> candidateTokens = state.Tokens.ToList();

            if (activeUserIds.Count > 0)
            {
                candidateTokens = state.Tokens
                    .Where(token => activeUserIds.Contains(token.UserId))
                    .ToList();
            }

            if (candidateTokens.Count == 0)
            {
                return coinsByUserId;
            }

            List<TokenStateDto> orderedTokens = candidateTokens
                .OrderByDescending(token => token.CellIndex)
                .ToList();

            for (int index = 0; index < orderedTokens.Count && index < 3; index++)
            {
                int position = index + 1;
                int coins = GetCoinsForPosition(position);

                if (coins <= 0)
                {
                    continue;
                }

                int userId = orderedTokens[index].UserId;

                if (!coinsByUserId.ContainsKey(userId))
                {
                    coinsByUserId[userId] = coins;
                }
            }

            return coinsByUserId;
        }

        private void FinalizeGame(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            if (session.FinishedAtUtc == DateTime.MinValue)
            {
                session.FinishedAtUtc = DateTime.UtcNow;
            }

            session.HasWinner = session.WinnerUserId != INVALID_USER_ID;

            if (session.HasWinner &&
                string.IsNullOrWhiteSpace(session.WinnerUserName))
            {
                session.WinnerUserName = GetUserNameOrDefault(session.GameId, session.WinnerUserId);
            }

            if (session.RewardsGranted)
            {
                return;
            }

            try
            {
                IDictionary<int, int> coinsByUserId = BuildCoinsDistribution(session);

                OperationResult<bool> result = gameResultsRepository.FinalizeGame(
                    session.GameId,
                    session.WinnerUserId,
                    coinsByUserId);

                if (!result.IsSuccess)
                {
                    Logger.ErrorFormat(
                        "FinalizeGame failed. GameId={0}, WinnerUserId={1}, Reason={2}",
                        session.GameId,
                        session.WinnerUserId,
                        result.ErrorMessage);

                    return;
                }

                session.RewardsGranted = true;
                gameSessionStore.UpdateSession(session);

                Logger.InfoFormat(
                    "Game finalized successfully. GameId={0}, WinnerUserId={1}, RewardedUsers={2}",
                    session.GameId,
                    session.WinnerUserId,
                    coinsByUserId.Count);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while finalizing game.", ex);
            }
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

            PlayerMoveResultDto moveDto = GameplayResponseBuilder.BuildPlayerMoveResultDto(
                previousTurnUserId,
                moveResult);

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

            if (!session.IsFinished && currentTurnUserId != INVALID_USER_ID)
            {
                session.CurrentTurnUserId = currentTurnUserId;
                session.CurrentTurnStartUtc = DateTime.UtcNow;
            }
            else
            {
                session.CurrentTurnStartUtc = DateTime.MinValue;
            }

            gameSessionStore.UpdateSession(session);

            NotifyTurnChanged(turnDto);

            if (!session.IsFinished && currentTurnUserId != INVALID_USER_ID)
            {
                StartOrResetTurnTimer(session);
            }
            else
            {
                StopTurnTimer(session.GameId);
            }
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

        public void RegisterTurnTimeout(int gameId)
        {
            if (gameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            Logger.InfoFormat(
                "RegisterTurnTimeout called for GameId={0}, but timeouts are handled by server timer.",
                gameId);
        }

        private void RegisterCallback(
            int gameId,
            int userId,
            string userName,
            IGameplayCallback callbackChannel)
        {
            var callbacksForGame = callbacksByGameId.GetOrAdd(
                gameId,
                _ => new ConcurrentDictionary<int, IGameplayCallback>());

            callbacksForGame[userId] = callbackChannel;

            var userNamesForGame = userNamesByGameId.GetOrAdd(
                gameId,
                _ => new ConcurrentDictionary<int, string>());

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
            bool gameFinishedByLastPlayer = false;

            GameSession session = null;

            try
            {
                if (gameSessionStore.TryGetSession(gameId, out session))
                {
                    var updatedPlayers = session.PlayerUserIds
                        .Where(id => id != userId && id != INVALID_USER_ID)
                        .ToList();

                    session.PlayerUserIds = updatedPlayers;

                    GameStateSnapshot state = GetCurrentStateSafe(session);
                    if (state != null)
                    {
                        wasCurrentTurn = state.CurrentTurnUserId == userId;

                        if (wasCurrentTurn)
                        {
                            newCurrentTurnUserId = FindNextPlayerId(session, userId);
                            if (newCurrentTurnUserId == INVALID_USER_ID)
                            {
                                newCurrentTurnUserId = null;
                            }
                        }
                    }

                    int activePlayersCount = session.PlayerUserIds
                        .Where(id => id != INVALID_USER_ID)
                        .Count();

                    if (activePlayersCount == 1 && !session.IsFinished)
                    {
                        int winnerUserId = session.PlayerUserIds
                            .First(id => id != INVALID_USER_ID);

                        session.IsFinished = true;
                        session.HasWinner = true;
                        session.WinnerUserId = winnerUserId;
                        session.WinnerUserName = GetUserNameOrDefault(gameId, winnerUserId);
                        session.EndReason = END_REASON_LAST_PLAYER_REMAINING;

                        if (session.FinishedAtUtc == DateTime.MinValue)
                        {
                            session.FinishedAtUtc = DateTime.UtcNow;
                        }

                        session.CurrentTurnStartUtc = DateTime.MinValue;

                        gameFinishedByLastPlayer = true;

                        gameSessionStore.UpdateSession(session);
                        StopTurnTimer(gameId);
                        FinalizeGame(session);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Error computing new turn or last-player rule after player left.", ex);
            }

            var leftDto = new PlayerLeftDto
            {
                GameId = gameId,
                UserId = userId,
                UserName = userName,
                WasCurrentTurnPlayer = wasCurrentTurn,
                NewCurrentTurnUserId = gameFinishedByLastPlayer
                    ? (int?)null
                    : newCurrentTurnUserId,
                Reason = leaveReason
            };

            NotifyPlayerLeft(leftDto);

            if (!gameFinishedByLastPlayer &&
                wasCurrentTurn &&
                newCurrentTurnUserId.HasValue &&
                session != null)
            {
                session.CurrentTurnUserId = newCurrentTurnUserId.Value;
                session.CurrentTurnStartUtc = DateTime.UtcNow;
                gameSessionStore.UpdateSession(session);

                StartOrResetTurnTimer(session);
            }

            if (!gameFinishedByLastPlayer &&
                wasCurrentTurn &&
                newCurrentTurnUserId.HasValue)
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

            StopTurnTimer(gameId);
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

        private void NotifyItemUsed(int gameId, ItemUsedNotificationDto notification)
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
                    callback => callback.OnItemUsed(notification));
            }
        }

        private void NotifyTurnTimerUpdated(TurnTimerUpdateDto timerInfo)
        {
            if (timerInfo == null)
            {
                return;
            }

            if (!callbacksByGameId.TryGetValue(timerInfo.GameId, out var callbacksForGame))
            {
                return;
            }

            foreach (var callbackEntry in callbacksForGame.ToArray())
            {
                InvokeCallbackSafely(
                    timerInfo.GameId,
                    callbackEntry.Key,
                    callbackEntry.Value,
                    callback => callback.OnTurnTimerUpdated(timerInfo));
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

        private void StartOrResetTurnTimer(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            int gameId = session.GameId;

            if (session.IsFinished || session.CurrentTurnUserId == INVALID_USER_ID)
            {
                StopTurnTimer(gameId);
                return;
            }

            TurnTimerState state = turnTimersByGameId.AddOrUpdate(
                gameId,
                _ => new TurnTimerState(gameId, session.CurrentTurnUserId, TURN_TIME_SECONDS),
                (_, existing) =>
                {
                    existing.CurrentTurnUserId = session.CurrentTurnUserId;
                    existing.RemainingSeconds = TURN_TIME_SECONDS;
                    existing.LastUpdatedUtc = DateTime.UtcNow;
                    return existing;
                });

            Timer timer = timersByGameId.GetOrAdd(
                gameId,
                _ =>
                {
                    var newTimer = new Timer(1000);
                    newTimer.AutoReset = true;
                    newTimer.Elapsed += (s, e) => OnServerTurnTimerTick(gameId);
                    return newTimer;
                });

            if (!timer.Enabled)
            {
                timer.Start();
            }

            Logger.InfoFormat(
                "StartOrResetTurnTimer: GameId={0}, CurrentTurnUserId={1}, Remaining={2}",
                gameId,
                state.CurrentTurnUserId,
                state.RemainingSeconds);

            NotifyTurnTimerUpdated(
                new TurnTimerUpdateDto
                {
                    GameId = gameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    RemainingSeconds = state.RemainingSeconds
                });
        }

        private void StopTurnTimer(int gameId)
        {
            turnTimersByGameId.TryRemove(gameId, out _);

            if (!timersByGameId.TryRemove(gameId, out Timer timer))
            {
                return;
            }

            try
            {
                timer.Stop();
                timer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warn("Error while stopping turn timer.", ex);
            }
        }

        private void OnServerTurnTimerTick(int gameId)
        {
            if (!turnTimersByGameId.TryGetValue(gameId, out TurnTimerState state))
            {
                return;
            }

            int newRemaining = state.RemainingSeconds - 1;
            state.RemainingSeconds = newRemaining;
            state.LastUpdatedUtc = DateTime.UtcNow;

            if (newRemaining <= 0)
            {
                StopTurnTimer(gameId);

                try
                {
                    ProcessTurnTimeout(gameId);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while processing turn timeout from server timer.", ex);
                }

                return;
            }

            NotifyTurnTimerUpdated(
                new TurnTimerUpdateDto
                {
                    GameId = gameId,
                    CurrentTurnUserId = state.CurrentTurnUserId,
                    RemainingSeconds = newRemaining
                });
        }
    }
}
