using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic.Gameplay;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class GameplayLogic
    {
        private const string ERROR_AT_LEAST_ONE_PLAYER_REQUIRED =
            "GameplayLogic requires at least one player.";

        private readonly BoardDefinitionDto _boardDefinition;

        private readonly List<int> _turnOrder;
        private readonly Dictionary<int, PlayerRuntimeState> _playersByUserId;

        private readonly object _syncRoot = new object();
        private readonly Random _random;

        private readonly IBoardNavigator _boardNavigator;
        private readonly IDiceManager _diceManager;
        private readonly IItemEffectManager _itemEffectManager;

        private readonly int _finalCellIndex;

        private int _currentTurnIndex;
        private bool _isFinished;

        public GameplayLogic(
            BoardDefinitionDto board,
            IEnumerable<int> playerUserIds)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (playerUserIds == null)
            {
                throw new ArgumentNullException(nameof(playerUserIds));
            }

            _boardDefinition = board;

            _turnOrder = playerUserIds
                .Distinct()
                .Where(id => id != GameplayLogicConstants.INVALID_USER_ID)
                .ToList();

            if (_turnOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    ERROR_AT_LEAST_ONE_PLAYER_REQUIRED);
            }

            _playersByUserId = _turnOrder
                .ToDictionary(
                    id => id,
                    id => new PlayerRuntimeState
                    {
                        UserId = id,
                        Position = GameplayLogicConstants.MIN_CELL_INDEX,
                        RemainingFrozenTurns = 0,
                        HasShield = false,
                        RemainingShieldTurns = 0,
                        PendingRocketBonus = 0,
                        ItemUsedThisTurn = false,
                        HasRolledThisTurn = false,
                        ConsecutiveTimeouts = 0
                    });

            _currentTurnIndex = 0;
            _isFinished = false;

            _random = new Random(unchecked((int)DateTime.UtcNow.Ticks));

            _boardNavigator = new BoardNavigator(_boardDefinition, _random);
            _diceManager = new DiceManager(_random);
            _itemEffectManager = new ItemEffectManager();

            _finalCellIndex = _boardNavigator.FinalCellIndex;
        }

        public TurnTimeoutResult HandleTurnTimeout()
        {
            lock (_syncRoot)
            {
                if (_isFinished)
                {
                    GameplayValidationHelper.ThrowGameAlreadyFinished();
                }

                GameplayValidationHelper.EnsureThereArePlayers(_turnOrder);

                int previousTurnUserId = _turnOrder[_currentTurnIndex];

                PlayerRuntimeState currentPlayer =
                    GameplayValidationHelper.GetPlayerStateOrThrow(
                        _playersByUserId,
                        previousTurnUserId);

                currentPlayer.ConsecutiveTimeouts++;

                bool playerKicked = false;
                int kickedUserId = GameplayLogicConstants.INVALID_USER_ID;
                int winnerUserId = GameplayLogicConstants.INVALID_USER_ID;

                if (currentPlayer.ConsecutiveTimeouts >=
                    GameplayLogicConstants.MAX_CONSECUTIVE_TIMEOUTS)
                {
                    playerKicked = true;
                    kickedUserId = previousTurnUserId;

                    _playersByUserId.Remove(previousTurnUserId);
                    _turnOrder.Remove(previousTurnUserId);

                    if (_turnOrder.Count <= 1)
                    {
                        _isFinished = true;

                        if (_turnOrder.Count == 1)
                        {
                            winnerUserId = _turnOrder[0];
                        }

                        return new TurnTimeoutResult
                        {
                            PreviousTurnUserId = previousTurnUserId,
                            CurrentTurnUserId =
                                GameplayLogicConstants.INVALID_USER_ID,
                            PlayerKicked = true,
                            KickedUserId = kickedUserId,
                            GameFinished = true,
                            WinnerUserId = winnerUserId
                        };
                    }

                    if (_currentTurnIndex >= _turnOrder.Count)
                    {
                        _currentTurnIndex =
                            _currentTurnIndex % _turnOrder.Count;
                    }
                }
                else
                {
                    AdvanceTurnAndResetFlags(previousTurnUserId);
                }

                int currentTurnUserId =
                    _isFinished || _turnOrder.Count == 0
                        ? GameplayLogicConstants.INVALID_USER_ID
                        : _turnOrder[_currentTurnIndex];

                return new TurnTimeoutResult
                {
                    PreviousTurnUserId = previousTurnUserId,
                    CurrentTurnUserId = currentTurnUserId,
                    PlayerKicked = playerKicked,
                    KickedUserId = kickedUserId,
                    GameFinished = _isFinished,
                    WinnerUserId = winnerUserId
                };
            }
        }

        public RollDiceResult RollDice(int userId, string diceCode)
        {
            lock (_syncRoot)
            {
                if (_isFinished)
                {
                    GameplayValidationHelper.ThrowGameAlreadyFinished();
                }

                GameplayValidationHelper.EnsureUserInGame(_turnOrder, userId);
                GameplayValidationHelper.EnsureIsUserTurn(
                    _turnOrder,
                    _currentTurnIndex,
                    userId);

                PlayerRuntimeState playerState =
                    GameplayValidationHelper.GetPlayerStateOrThrow(
                        _playersByUserId,
                        userId);

                playerState.ConsecutiveTimeouts = 0;

                if (playerState.RemainingFrozenTurns > 0)
                {
                    return HandleFrozenPlayerRoll(playerState, userId);
                }

                playerState.HasRolledThisTurn = true;

                int diceValue = _diceManager.GetDiceValue(
                    playerState,
                    diceCode);

                int fromCellIndex = playerState.Position;

                TentativeMoveResult tentativeMove = CalculateTentativeTarget(
                    playerState,
                    fromCellIndex,
                    diceValue);

                int tentativeTarget = tentativeMove.TargetCellIndex;
                bool usedRocket = tentativeMove.UsedRocket;
                bool rocketIgnored = tentativeMove.RocketIgnored;

                if (IsRollTooHigh(tentativeTarget))
                {
                    string extraInfo =
                        GameplayLogicConstants.ROLL_TOO_HIGH_NO_MOVE;

                    if (rocketIgnored)
                    {
                        extraInfo = AppendToken(
                            GameplayLogicConstants.ROCKET_IGNORED,
                            GameplayLogicConstants.ROLL_TOO_HIGH_NO_MOVE);
                    }

                    AdvanceTurnAndResetFlags(userId);

                    return new RollDiceResult
                    {
                        DiceValue = diceValue,
                        FromCellIndex = fromCellIndex,
                        ToCellIndex = fromCellIndex,
                        IsGameOver = _isFinished,
                        ExtraInfo = extraInfo,
                        UsedRocket = false,
                        RocketIgnored = rocketIgnored,
                        MessageIndex = null,
                        GrantedItemCode = null,
                        GrantedDiceCode = null
                    };
                }

                MovementResult movementResult = ApplyMovement(
                    playerState,
                    tentativeTarget);

                // clave para que el jugador avance correctamente
                playerState.Position = movementResult.FinalPosition;

                bool isGameOver = false;

                if (_finalCellIndex > 0 &&
                    movementResult.FinalPosition >= _finalCellIndex)
                {
                    _isFinished = true;
                    isGameOver = true;

                    movementResult.ExtraInfo = AppendToken(
                        movementResult.ExtraInfo,
                        GameplayLogicConstants.WIN);
                }

                string finalExtraInfo = BuildFinalExtraInfo(
                    movementResult.ExtraInfo,
                    usedRocket,
                    rocketIgnored);

                bool hasExtraRoll =
                    movementResult.ShouldGrantExtraRoll && !isGameOver;

                if (hasExtraRoll)
                {
                    ResetPlayerForExtraRoll(playerState);
                }
                else
                {
                    AdvanceTurnAndResetFlags(userId);
                }

                return new RollDiceResult
                {
                    DiceValue = diceValue,
                    FromCellIndex = fromCellIndex,
                    ToCellIndex = movementResult.FinalPosition,
                    IsGameOver = isGameOver,
                    ExtraInfo = finalExtraInfo,
                    UsedRocket = usedRocket,
                    RocketIgnored = rocketIgnored,
                    MessageIndex = movementResult.MessageIndex,
                    GrantedItemCode = movementResult.GrantedItemCode,
                    GrantedDiceCode = movementResult.GrantedDiceCode
                };
            }
        }

        public GameStateSnapshot GetCurrentState()
        {
            lock (_syncRoot)
            {
                int currentTurnUserId = _turnOrder[_currentTurnIndex];

                List<TokenStateDto> tokens = _playersByUserId
                    .Select(pair =>
                    {
                        PlayerRuntimeState state = pair.Value;

                        return new TokenStateDto
                        {
                            UserId = pair.Key,
                            CellIndex = state.Position,
                            HasShield = state.HasShield,
                            RemainingShieldTurns = state.RemainingShieldTurns,
                            RemainingFrozenTurns = state.RemainingFrozenTurns,
                            HasPendingRocketBonus =
                                state.PendingRocketBonus > 0
                        };
                    })
                    .ToList();

                return new GameStateSnapshot
                {
                    CurrentTurnUserId = currentTurnUserId,
                    IsFinished = _isFinished,
                    Tokens = tokens
                };
            }
        }

        public ItemEffectResult UseItem(
            int userId,
            string itemCode,
            int? targetUserId)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentNullException(nameof(itemCode));
            }

            lock (_syncRoot)
            {
                if (_isFinished)
                {
                    GameplayValidationHelper.ThrowGameAlreadyFinished();
                }

                GameplayValidationHelper.EnsureUserInGame(_turnOrder, userId);
                GameplayValidationHelper.EnsureIsUserTurn(
                    _turnOrder,
                    _currentTurnIndex,
                    userId);

                PlayerRuntimeState currentPlayer =
                    GameplayValidationHelper.GetPlayerStateOrThrow(
                        _playersByUserId,
                        userId);

                ItemUsageGuard.EnsurePlayerCanUseItem(currentPlayer);

                ItemEffectResult result = _itemEffectManager.UseItem(
                    itemCode,
                    userId,
                    targetUserId,
                    _playersByUserId,
                    ApplyJumpEffectsIfAny);

                if (!result.WasBlockedByShield)
                {
                    currentPlayer.ItemUsedThisTurn = true;
                }

                return result;
            }
        }

        private RollDiceResult HandleFrozenPlayerRoll(
            PlayerRuntimeState playerState,
            int userId)
        {
            playerState.RemainingFrozenTurns--;

            if (playerState.RemainingFrozenTurns < 0)
            {
                playerState.RemainingFrozenTurns = 0;
            }

            string frozenExtraInfo =
                GameplayLogicConstants.FROZEN_SKIP_TURN;

            AdvanceTurnAndResetFlags(userId);

            return new RollDiceResult
            {
                DiceValue = 0,
                FromCellIndex = playerState.Position,
                ToCellIndex = playerState.Position,
                IsGameOver = _isFinished,
                ExtraInfo = frozenExtraInfo,
                UsedRocket = false,
                RocketIgnored = false,
                MessageIndex = null,
                GrantedItemCode = null,
                GrantedDiceCode = null
            };
        }

        private TentativeMoveResult CalculateTentativeTarget(
            PlayerRuntimeState playerState,
            int fromCellIndex,
            int diceValue)
        {
            int targetCellIndex = fromCellIndex + diceValue;

            if (targetCellIndex < GameplayLogicConstants.MIN_BOARD_CELL)
            {
                targetCellIndex = GameplayLogicConstants.MIN_BOARD_CELL;
            }

            bool usedRocket = false;
            bool rocketIgnored = false;

            if (playerState.PendingRocketBonus > 0)
            {
                int targetWithRocket = fromCellIndex
                    + diceValue
                    + playerState.PendingRocketBonus;

                if (_finalCellIndex > 0 &&
                    targetWithRocket <= _finalCellIndex)
                {
                    targetCellIndex = targetWithRocket;
                    usedRocket = true;
                }
                else
                {
                    rocketIgnored = true;
                }

                playerState.PendingRocketBonus = 0;
            }

            return new TentativeMoveResult
            {
                TargetCellIndex = targetCellIndex,
                UsedRocket = usedRocket,
                RocketIgnored = rocketIgnored
            };
        }

        private bool IsRollTooHigh(int tentativeTarget)
        {
            return _finalCellIndex > 0 &&
                   tentativeTarget > _finalCellIndex;
        }

        private MovementResult ApplyMovement(
            PlayerRuntimeState playerState,
            int tentativeTarget)
        {
            int finalTarget = _boardNavigator.ApplyJumpEffectsIfAny(
                playerState,
                tentativeTarget);

            string extraInfo = BuildJumpExtraInfo(
                playerState,
                tentativeTarget);

            SpecialCellResult specialCellResult =
                _boardNavigator.ApplySpecialCellIfAny(
                    playerState,
                    finalTarget,
                    extraInfo);

            return new MovementResult
            {
                FinalPosition = specialCellResult.FinalCellIndex,
                ExtraInfo = specialCellResult.ExtraInfo,
                MessageIndex = specialCellResult.MessageIndex,
                ShouldGrantExtraRoll = specialCellResult.GrantsExtraRoll,
                GrantedItemCode = specialCellResult.GrantedItemCode,
                GrantedDiceCode = specialCellResult.GrantedDiceCode
            };
        }

        private string BuildJumpExtraInfo(
            PlayerRuntimeState playerState,
            int tentativeTarget)
        {
            JumpInfo jumpInfo = GetJumpInfo(tentativeTarget);

            if (!jumpInfo.HasJump)
            {
                return string.Empty;
            }

            bool isLadder = jumpInfo.DestinationIndex > tentativeTarget;
            bool isSnake = jumpInfo.DestinationIndex < tentativeTarget;

            if (isSnake && playerState.HasShield)
            {
                return GameplayLogicConstants.SNAKE_BLOCKED_BY_SHIELD;
            }

            if (isLadder)
            {
                return GameplayLogicConstants.LADDER;
            }

            if (isSnake)
            {
                return GameplayLogicConstants.SNAKE;
            }

            return GameplayLogicConstants.JUMP_SAME;
        }

        private string BuildFinalExtraInfo(
            string extraInfo,
            bool usedRocket,
            bool rocketIgnored)
        {
            string result = extraInfo;

            if (usedRocket)
            {
                result = AppendToken(
                    GameplayLogicConstants.ROCKET_USED,
                    result);
            }
            else if (rocketIgnored)
            {
                result = AppendToken(
                    GameplayLogicConstants.ROCKET_IGNORED,
                    result);
            }

            return result;
        }

        private int ApplyJumpEffectsIfAny(
            PlayerRuntimeState targetPlayer,
            int candidatePosition)
        {
            return _boardNavigator.ApplyJumpEffectsIfAny(
                targetPlayer,
                candidatePosition);
        }

        private void AdvanceTurnAndResetFlags(int currentUserId)
        {
            PlayerRuntimeState currentPlayer;
            if (!_playersByUserId.TryGetValue(
                    currentUserId,
                    out currentPlayer))
            {
                return;
            }

            if (currentPlayer.HasShield &&
                currentPlayer.RemainingShieldTurns > 0)
            {
                currentPlayer.RemainingShieldTurns--;

                if (currentPlayer.RemainingShieldTurns <= 0)
                {
                    currentPlayer.RemainingShieldTurns = 0;
                    currentPlayer.HasShield = false;
                }
            }

            currentPlayer.ItemUsedThisTurn = false;
            currentPlayer.HasRolledThisTurn = false;

            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
        }

        private JumpInfo GetJumpInfo(int startIndex)
        {
            var dummyPlayer = new PlayerRuntimeState
            {
                HasShield = false
            };

            int result = _boardNavigator.ApplyJumpEffectsIfAny(
                dummyPlayer,
                startIndex);

            if (result != startIndex)
            {
                return new JumpInfo
                {
                    HasJump = true,
                    DestinationIndex = result
                };
            }

            return new JumpInfo
            {
                HasJump = false,
                DestinationIndex = startIndex
            };
        }

        private static string AppendToken(
            string baseInfo,
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return baseInfo ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(baseInfo))
            {
                return token;
            }

            return baseInfo +
                   GameplayLogicConstants.TOKEN_SEPARATOR +
                   token;
        }

        private static void ResetPlayerForExtraRoll(
            PlayerRuntimeState playerState)
        {
            if (playerState == null)
            {
                return;
            }

            playerState.HasRolledThisTurn = false;
            playerState.ItemUsedThisTurn = false;
        }

        private sealed class MovementResult
        {
            public int FinalPosition { get; set; }

            public string ExtraInfo { get; set; }

            public int? MessageIndex { get; set; }

            public bool ShouldGrantExtraRoll { get; set; }

            public string GrantedItemCode { get; set; }

            public string GrantedDiceCode { get; set; }
        }

        private sealed class TentativeMoveResult
        {
            public int TargetCellIndex { get; set; }

            public bool UsedRocket { get; set; }

            public bool RocketIgnored { get; set; }
        }

        private sealed class JumpInfo
        {
            public bool HasJump { get; set; }

            public int DestinationIndex { get; set; }
        }
    }
}
