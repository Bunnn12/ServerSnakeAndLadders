using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic.Gameplay;
using SnakesAndLadders.Services.Wcf.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GameplayService : IGameplayService
    {
        private const string ERROR_REQUEST_NULL =
            "Request cannot be null.";

        private const string ERROR_GAME_ID_INVALID =
            "GameId must be greater than zero.";

        private const string ERROR_USER_ID_INVALID =
            "UserId is invalid.";

        private const string ERROR_SESSION_NOT_FOUND =
            "Game session not found for this game.";

        private const string ERROR_UNEXPECTED_ROLL =
            "Unexpected error while processing dice roll.";

        private const string ERROR_UNEXPECTED_STATE =
            "Unexpected error while retrieving game state.";

        private const string ERROR_JOIN_INVALID =
            "GameId must be greater than zero and UserId must be non-zero.";

        private const string ERROR_LEAVE_INVALID =
            "GameId must be greater than zero and UserId must be non-zero.";

        private const string ERROR_USE_ITEM_INVALID_SLOT =
            "Item slot number is invalid.";

        private const string ERROR_USE_ITEM_NO_ITEM_IN_SLOT =
            "No item is equipped in the selected slot.";

        private const string ERROR_USE_ITEM_NO_QUANTITY =
            "User does not have any remaining units of the selected item.";

        private const string ERROR_USE_DICE_INVALID_SLOT =
            "Dice slot number is invalid.";

        private const string ERROR_USE_DICE_NO_DICE_IN_SLOT =
            "No dice is equipped in the selected slot.";

        private const string ERROR_USE_DICE_NO_QUANTITY =
            "User does not have any remaining units of the selected dice.";

        private const string ERROR_GRANT_REWARD_FAILED =
            "Error while granting special cell reward.";

        private const string ERROR_INVENTORY_UNAVAILABLE =
            "No se pudo acceder al inventario. Intenta de nuevo más tarde.";

        private const string ERROR_DICE_INVENTORY_UNAVAILABLE =
            "No se pudo acceder a los dados equipados. Intenta de nuevo más tarde.";

        private const string TURN_REASON_NORMAL = "NORMAL";
        private const string TURN_REASON_PLAYER_LEFT = "PLAYER_LEFT";
        private const string TURN_REASON_TIMEOUT_SKIP = "TIMEOUT_SKIP";
        private const string TURN_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";

        private const string LEAVE_REASON_PLAYER_REQUEST = "PLAYER_LEFT";
        private const string LEAVE_REASON_DISCONNECTED = "DISCONNECTED";
        private const string LEAVE_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";

        private const string EFFECT_TOKEN_ROCKET_USED = "ROCKET_USED";
        private const string EFFECT_TOKEN_ROCKET_IGNORED = "ROCKET_IGNORED";

        private const string END_REASON_BOARD_WIN = "BOARD_WIN";
        private const string END_REASON_TIMEOUT_KICK = "TIMEOUT_KICK";
        private const string END_REASON_LAST_PLAYER_REMAINING =
            "LAST_PLAYER_REMAINING";

        private const int INVALID_USER_ID = 0;

        private const byte MIN_ITEM_SLOT = 1;
        private const byte MAX_ITEM_SLOT = 3;

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private const int COINS_FIRST_PLACE = 50;
        private const int COINS_SECOND_PLACE = 30;
        private const int COINS_THIRD_PLACE = 10;

        private static readonly ILog Logger =
            LogManager.GetLogger(typeof(GameplayService));

        private readonly IGameSessionStore _gameSessionStore;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IGameResultsRepository _gameResultsRepository;
        private readonly IGameplayAppService _gameplayAppService;

        private readonly GameplayInventoryService _inventoryService;
        private readonly GameplayCallbackManager _callbackManager;
        private readonly TurnTimerManager _turnTimerManager;

        public GameplayService(
            IGameSessionStore gameSessionStore,
            IInventoryRepository inventoryRepository,
            IGameResultsRepository gameResultsRepository,
            IGameplayAppService gameplayAppService,
            IAppLogger appLogger)
        {
            _gameSessionStore = gameSessionStore
                ?? throw new ArgumentNullException(nameof(gameSessionStore));

            _inventoryRepository = inventoryRepository
                ?? throw new ArgumentNullException(nameof(inventoryRepository));

            _gameResultsRepository = gameResultsRepository
                ?? throw new ArgumentNullException(nameof(gameResultsRepository));

            _gameplayAppService = gameplayAppService
                ?? throw new ArgumentNullException(nameof(gameplayAppService));

            _inventoryService = new GameplayInventoryService(
                _inventoryRepository,
                Logger);

            _callbackManager = new GameplayCallbackManager();
            _turnTimerManager = new TurnTimerManager(Logger);

            _turnTimerManager.TurnTimedOut += OnTurnTimedOut;
            _turnTimerManager.TimerUpdated += OnTurnTimerUpdated;
        }

        public RollDiceResponseDto RollDice(RollDiceRequestDto request)
        {
            ValidateRollDiceRequest(request);

            GameSession session = GetSessionOrThrow(request.GameId);

            InventoryDiceDto equippedDice = ResolveEquippedDice(request);
            string diceCode = equippedDice?.DiceCode;
            int? diceIdToConsume = equippedDice?.DiceId;

            try
            {
                RollDiceResult moveResult = _gameplayAppService.RollDice(
                    request.GameId,
                    request.PlayerUserId,
                    diceCode);

                _inventoryService.GrantRewardsFromSpecialCells(
                    request.PlayerUserId,
                    moveResult,
                    ERROR_GRANT_REWARD_FAILED);

                ConsumeDiceIfNeeded(
                    request,
                    diceCode,
                    diceIdToConsume);

                _inventoryService.HandlePendingRocketConsumption(
                    session.GameId,
                    request.PlayerUserId,
                    moveResult,
                    EFFECT_TOKEN_ROCKET_USED,
                    EFFECT_TOKEN_ROCKET_IGNORED);

                UpdateSessionAfterRoll(
                    session,
                    request.PlayerUserId,
                    moveResult);

                RollDiceResponseDto rollResponse =
                    GameplayResponseBuilder.BuildRollDiceResponse(
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

            if (!_gameSessionStore.TryGetSession(request.GameId, out GameSession session))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            try
            {
                GameStateSnapshot state =
                    _gameplayAppService.GetCurrentState(request.GameId);

                int remainingSeconds = 0;

                if (!session.IsFinished &&
                    state.CurrentTurnUserId != INVALID_USER_ID)
                {
                    remainingSeconds =
                        _turnTimerManager.GetRemainingSecondsOrDefault(
                            session.GameId,
                            state.CurrentTurnUserId);

                    if (remainingSeconds == 0)
                    {
                        _turnTimerManager.StartOrResetTurnTimer(
                            session.GameId,
                            state.CurrentTurnUserId);

                        remainingSeconds =
                            _turnTimerManager.GetRemainingSecondsOrDefault(
                                session.GameId,
                                state.CurrentTurnUserId);
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

            if (request.ItemSlotNumber < MIN_ITEM_SLOT ||
                request.ItemSlotNumber > MAX_ITEM_SLOT)
            {
                throw new FaultException(ERROR_USE_ITEM_INVALID_SLOT);
            }

            GameSession session = GetSessionOrThrow(request.GameId);

            InventoryItemDto equippedItem = ResolveEquippedItem(request);

            try
            {
                ItemEffectResult effectResult = _gameplayAppService.UseItem(
                    request.GameId,
                    request.PlayerUserId,
                    equippedItem.ObjectCode,
                    request.TargetUserId);

                bool shouldConsume = ShouldConsumeItem(
                    equippedItem,
                    effectResult);

                TrackRocketIfNeeded(
                    session,
                    request,
                    equippedItem,
                    effectResult);

                ConsumeItemIfNeeded(
                    request,
                    equippedItem,
                    shouldConsume);

                GetGameStateResponseDto updatedGameState = GetGameState(
                    new GetGameStateRequestDto
                    {
                        GameId = request.GameId
                    });

                UseItemResponseDto response = BuildUseItemResponse(
                    request,
                    equippedItem,
                    effectResult,
                    updatedGameState);

                ItemUsedNotificationDto notification =
                    BuildItemUsedNotification(
                        request,
                        equippedItem,
                        effectResult,
                        updatedGameState);

                _callbackManager.NotifyItemUsed(
                    request.GameId,
                    notification,
                    (failedUserId, ex) =>
                        HandleCallbackFailure(
                            request.GameId,
                            failedUserId,
                            ex));

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
                Logger.Error(
                    "Unexpected error while processing item usage.",
                    ex);
                throw new FaultException(
                    "Unexpected error while processing item usage.");
            }
        }

        public void JoinGame(int gameId, int userId, string userName)
        {
            if (gameId <= 0 || userId == INVALID_USER_ID)
            {
                throw new FaultException(ERROR_JOIN_INVALID);
            }

            if (!_gameSessionStore.TryGetSession(gameId, out GameSession _))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            IGameplayCallback callbackChannel = OperationContext.Current
                .GetCallbackChannel<IGameplayCallback>();

            _callbackManager.RegisterCallback(
                gameId,
                userId,
                userName,
                callbackChannel);

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

            HandlePlayerLeaving(
                gameId,
                userId,
                safeReason);

            Logger.InfoFormat(
                "LeaveGame: user left callbacks. GameId={0}, UserId={1}, " +
                "Reason={2}",
                gameId,
                userId,
                safeReason);
        }

        public void RegisterTurnTimeout(int gameId)
        {
            if (gameId <= 0)
            {
                throw new FaultException(ERROR_GAME_ID_INVALID);
            }

            Logger.InfoFormat(
                "RegisterTurnTimeout called for GameId={0}, but timeouts are " +
                "handled by server timer.",
                gameId);
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

            TurnTimeoutResult result;

            try
            {
                result = _gameplayAppService.HandleTurnTimeout(gameId);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(
                    "Business validation error while processing turn timeout.",
                    ex);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "Unexpected error while processing turn timeout.",
                    ex);
                return;
            }

            if (result.PlayerKicked &&
                result.KickedUserId != INVALID_USER_ID)
            {
                List<int> updatedPlayers = session.PlayerUserIds
                    .Where(id =>
                        id != result.KickedUserId &&
                        id != INVALID_USER_ID)
                    .ToList();

                session.PlayerUserIds = updatedPlayers;
            }

            session.IsFinished = result.GameFinished;

            if (result.WinnerUserId != INVALID_USER_ID)
            {
                session.HasWinner = true;
                session.WinnerUserId = result.WinnerUserId;
                session.WinnerUserName =
                    _callbackManager.GetUserNameOrDefault(
                        gameId,
                        result.WinnerUserId);
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

            _gameSessionStore.UpdateSession(session);

            if (session.IsFinished)
            {
                FinalizeGame(session);
            }

            if (result.PlayerKicked &&
                result.KickedUserId != INVALID_USER_ID)
            {
                string userName = _callbackManager.GetUserNameOrDefault(
                    gameId,
                    result.KickedUserId);

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

                _callbackManager.NotifyPlayerLeft(
                    leftDto,
                    (failedUserId, ex) =>
                        HandleCallbackFailure(
                            gameId,
                            failedUserId,
                            ex));
            }

            if (!session.IsFinished &&
                result.CurrentTurnUserId != INVALID_USER_ID)
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

                _callbackManager.NotifyTurnChanged(
                    turnDto,
                    (failedUserId, ex) =>
                        HandleCallbackFailure(
                            gameId,
                            failedUserId,
                            ex));

                _turnTimerManager.StartOrResetTurnTimer(
                    session.GameId,
                    result.CurrentTurnUserId);
            }
            else
            {
                _turnTimerManager.StopTurnTimer(gameId);
            }
        }

        private void OnTurnTimedOut(int gameId)
        {
            try
            {
                ProcessTurnTimeout(gameId);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "Error while processing turn timeout from server timer.",
                    ex);
            }
        }

        private void OnTurnTimerUpdated(TurnTimerUpdateDto timerInfo)
        {
            _callbackManager.NotifyTurnTimerUpdated(
                timerInfo,
                (failedUserId, ex) =>
                    HandleCallbackFailure(
                        timerInfo.GameId,
                        failedUserId,
                        ex));
        }

        private GameSession GetSessionOrThrow(int gameId)
        {
            if (!_gameSessionStore.TryGetSession(gameId, out GameSession session))
            {
                throw new FaultException(ERROR_SESSION_NOT_FOUND);
            }

            return session;
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

        private InventoryDiceDto ResolveEquippedDice(RollDiceRequestDto request)
        {
            if (!request.DiceSlotNumber.HasValue)
            {
                return null;
            }

            return _inventoryService.ResolveEquippedDiceForSlot(
                request.PlayerUserId,
                request.DiceSlotNumber.Value,
                ERROR_USE_DICE_NO_DICE_IN_SLOT,
                ERROR_USE_DICE_NO_QUANTITY,
                ERROR_DICE_INVENTORY_UNAVAILABLE);
        }

        private void ConsumeDiceIfNeeded(
            RollDiceRequestDto request,
            string diceCode,
            int? diceIdToConsume)
        {
            if (!diceIdToConsume.HasValue ||
                string.IsNullOrWhiteSpace(diceCode))
            {
                return;
            }

            try
            {
                _inventoryRepository.ConsumeDice(
                    request.PlayerUserId,
                    diceIdToConsume.Value);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "Error while consuming dice after roll.",
                    ex);
            }
        }

        private void UpdateSessionAfterRoll(
            GameSession session,
            int playerUserId,
            RollDiceResult moveResult)
        {
            session.IsFinished = moveResult.IsGameOver;

            if (moveResult.IsGameOver)
            {
                session.HasWinner = true;
                session.WinnerUserId = playerUserId;
                session.WinnerUserName =
                    _callbackManager.GetUserNameOrDefault(
                        session.GameId,
                        playerUserId);
                session.EndReason = END_REASON_BOARD_WIN;
                session.FinishedAtUtc = DateTime.UtcNow;
                session.CurrentTurnStartUtc = DateTime.MinValue;

                _turnTimerManager.StopTurnTimer(session.GameId);
                _gameSessionStore.UpdateSession(session);
                FinalizeGame(session);
            }
            else
            {
                _gameSessionStore.UpdateSession(session);
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

            PlayerMoveResultDto moveDto =
                GameplayResponseBuilder.BuildPlayerMoveResultDto(
                    previousTurnUserId,
                    moveResult);

            _callbackManager.NotifyPlayerMoved(
                session.GameId,
                moveDto,
                (failedUserId, ex) =>
                    HandleCallbackFailure(
                        session.GameId,
                        failedUserId,
                        ex));

            GameStateSnapshot stateAfterMove =
                GetCurrentStateSafe(session.GameId);

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

            UpdateSessionTurnInfo(
                session,
                currentTurnUserId);

            _callbackManager.NotifyTurnChanged(
                turnDto,
                (failedUserId, ex) =>
                    HandleCallbackFailure(
                        session.GameId,
                        failedUserId,
                        ex));

            if (!session.IsFinished &&
                currentTurnUserId != INVALID_USER_ID)
            {
                _turnTimerManager.StartOrResetTurnTimer(
                    session.GameId,
                    currentTurnUserId);
            }
            else
            {
                _turnTimerManager.StopTurnTimer(session.GameId);
            }
        }

        private static void UpdateSessionTurnInfo(
            GameSession session,
            int currentTurnUserId)
        {
            if (!session.IsFinished &&
                currentTurnUserId != INVALID_USER_ID)
            {
                session.CurrentTurnUserId = currentTurnUserId;
                session.CurrentTurnStartUtc = DateTime.UtcNow;
            }
            else
            {
                session.CurrentTurnStartUtc = DateTime.MinValue;
            }
        }

        private GameStateSnapshot GetCurrentStateSafe(int gameId)
        {
            try
            {
                return _gameplayAppService.GetCurrentState(gameId);
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    "Failed to retrieve current state after move.",
                    ex);
                return null;
            }
        }

        private InventoryItemDto ResolveEquippedItem(UseItemRequestDto request)
        {
            try
            {
                return _inventoryService.ResolveEquippedItemForSlot(
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
        }

        private static bool ShouldConsumeItem(
            InventoryItemDto equippedItem,
            ItemEffectResult effectResult)
        {
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
                return false;
            }

            return shouldConsume;
        }

        private void TrackRocketIfNeeded(
            GameSession session,
            UseItemRequestDto request,
            InventoryItemDto equippedItem,
            ItemEffectResult effectResult)
        {
            bool isRocket = effectResult.EffectType == ItemEffectType.Rocket;

            if (!isRocket || effectResult.WasBlockedByShield)
            {
                return;
            }

            _inventoryService.TrackPendingRocket(
                session.GameId,
                request.PlayerUserId,
                request.ItemSlotNumber,
                equippedItem.ObjectId,
                equippedItem.ObjectCode);
        }

        private void ConsumeItemIfNeeded(
            UseItemRequestDto request,
            InventoryItemDto equippedItem,
            bool shouldConsume)
        {
            if (!shouldConsume)
            {
                return;
            }

            try
            {
                _inventoryRepository.ConsumeItem(
                    request.PlayerUserId,
                    equippedItem.ObjectId);

                _inventoryRepository.RemoveItemFromSlot(
                    request.PlayerUserId,
                    request.ItemSlotNumber);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "Unexpected error while consuming item after usage.",
                    ex);
            }
        }

        private static UseItemResponseDto BuildUseItemResponse(
            UseItemRequestDto request,
            InventoryItemDto equippedItem,
            ItemEffectResult effectResult,
            GetGameStateResponseDto updatedGameState)
        {
            return new UseItemResponseDto
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
        }

        private static ItemUsedNotificationDto BuildItemUsedNotification(
            UseItemRequestDto request,
            InventoryItemDto equippedItem,
            ItemEffectResult effectResult,
            GetGameStateResponseDto updatedGameState)
        {
            return new ItemUsedNotificationDto
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
        }

        private int GetCoinsForPosition(int position)
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

            GameStateSnapshot state =
                GetCurrentStateSafe(session.GameId);

            if (state == null ||
                state.Tokens == null ||
                state.Tokens.Count == 0)
            {
                return coinsByUserId;
            }

            IReadOnlyCollection<int> activeUserIds =
                _callbackManager.GetActiveUserIds(session.GameId);

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

            for (int index = 0;
                index < orderedTokens.Count && index < 3;
                index++)
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
                session.WinnerUserName =
                    _callbackManager.GetUserNameOrDefault(
                        session.GameId,
                        session.WinnerUserId);
            }

            if (session.RewardsGranted)
            {
                return;
            }

            try
            {
                IDictionary<int, int> coinsByUserId =
                    BuildCoinsDistribution(session);

                OperationResult<bool> result =
                    _gameResultsRepository.FinalizeGame(
                        session.GameId,
                        session.WinnerUserId,
                        coinsByUserId);

                if (!result.IsSuccess)
                {
                    Logger.ErrorFormat(
                        "FinalizeGame failed. GameId={0}, WinnerUserId={1}, " +
                        "Reason={2}",
                        session.GameId,
                        session.WinnerUserId,
                        result.ErrorMessage);

                    return;
                }

                session.RewardsGranted = true;
                _gameSessionStore.UpdateSession(session);

                Logger.InfoFormat(
                    "Game finalized successfully. GameId={0}, WinnerUserId={1}," +
                    " RewardedUsers={2}",
                    session.GameId,
                    session.WinnerUserId,
                    coinsByUserId.Count);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "Unexpected error while finalizing game.",
                    ex);
            }
        }

        private void HandlePlayerLeaving(
            int gameId,
            int userId,
            string leaveReason)
        {
            bool wasRemoved =
                _callbackManager.TryRemoveCallback(gameId, userId);

            if (!wasRemoved)
            {
                return;
            }

            string userName = _callbackManager.GetUserNameOrDefault(
                gameId,
                userId);

            bool wasCurrentTurn = false;
            int? newCurrentTurnUserId = null;
            bool gameFinishedByLastPlayer = false;

            GameSession session = null;

            try
            {
                if (_gameSessionStore.TryGetSession(gameId, out session))
                {
                    List<int> updatedPlayers = session.PlayerUserIds
                        .Where(id =>
                            id != userId &&
                            id != INVALID_USER_ID)
                        .ToList();

                    session.PlayerUserIds = updatedPlayers;

                    GameStateSnapshot state =
                        GetCurrentStateSafe(session.GameId);

                    if (state != null)
                    {
                        wasCurrentTurn =
                            state.CurrentTurnUserId == userId;

                        if (wasCurrentTurn)
                        {
                            int nextPlayerId = FindNextPlayerId(
                                session,
                                userId);

                            if (nextPlayerId != INVALID_USER_ID)
                            {
                                newCurrentTurnUserId = nextPlayerId;
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
                        session.WinnerUserName =
                            _callbackManager.GetUserNameOrDefault(
                                gameId,
                                winnerUserId);
                        session.EndReason = END_REASON_LAST_PLAYER_REMAINING;

                        if (session.FinishedAtUtc == DateTime.MinValue)
                        {
                            session.FinishedAtUtc = DateTime.UtcNow;
                        }

                        session.CurrentTurnStartUtc = DateTime.MinValue;

                        gameFinishedByLastPlayer = true;

                        _gameSessionStore.UpdateSession(session);
                        _turnTimerManager.StopTurnTimer(gameId);
                        FinalizeGame(session);
                    }
                }
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    "Error computing new turn or last-player rule after " +
                    "player left.",
                    ex);
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

            _callbackManager.NotifyPlayerLeft(
                leftDto,
                (failedUserId, ex) =>
                    HandleCallbackFailure(
                        gameId,
                        failedUserId,
                        ex));

            if (!gameFinishedByLastPlayer &&
                wasCurrentTurn &&
                newCurrentTurnUserId.HasValue &&
                session != null)
            {
                session.CurrentTurnUserId = newCurrentTurnUserId.Value;
                session.CurrentTurnStartUtc = DateTime.UtcNow;
                _gameSessionStore.UpdateSession(session);

                _turnTimerManager.StartOrResetTurnTimer(
                    session.GameId,
                    newCurrentTurnUserId.Value);
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

                _callbackManager.NotifyTurnChanged(
                    turnDto,
                    (failedUserId, ex) =>
                        HandleCallbackFailure(
                            gameId,
                            failedUserId,
                            ex));
            }

            CleanupDictionariesIfEmpty(gameId);
        }

        private static int FindNextPlayerId(
            GameSession session,
            int leavingUserId)
        {
            List<int> orderedPlayers = session.PlayerUserIds
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

        private void CleanupDictionariesIfEmpty(int gameId)
        {
            _callbackManager.CleanupIfEmpty(gameId);
        }

        private void HandleCallbackFailure(
            int gameId,
            int userId,
            Exception ex)
        {
            Logger.WarnFormat(
                "Callback invocation failed. GameId={0}, UserId={1}, " +
                "Exception={2}",
                gameId,
                userId,
                ex);

            HandlePlayerLeaving(
                gameId,
                userId,
                LEAVE_REASON_DISCONNECTED);
        }
    }
}
