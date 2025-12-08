using System;
using System.Collections.Generic;
using System.Linq;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;

namespace SnakesAndLadders.Services.Logic
{
    using SnakesAndLadders.Services.Logic.Gameplay;

    public sealed class GameplayLogic
    {
        private const int INVALID_USER_ID = 0;

        private const int MAX_CONSECUTIVE_TIMEOUTS = 3;

        private const int MIN_CELL_INDEX = 0;
        private const int MIN_BOARD_CELL = 1;

        private const string EXTRA_INFO_ROLL_TOO_HIGH_NO_MOVE = "RollTooHigh_NoMove";
        private const string EXTRA_INFO_LADDER = "Ladder";
        private const string EXTRA_INFO_SNAKE = "Snake";
        private const string EXTRA_INFO_JUMP_SAME = "JumpButSameIndex";
        private const string EXTRA_INFO_WIN = "Win";
        private const string EXTRA_INFO_FROZEN_SKIP_TURN = "Frozen_SkipTurn";
        private const string EXTRA_INFO_SNAKE_BLOCKED_BY_SHIELD = "Snake_BlockedByShield";
        private const string EXTRA_INFO_ROCKET_USED = "Rocket_Used";
        private const string EXTRA_INFO_ROCKET_IGNORED = "Rocket_Ignored";

        private readonly BoardDefinitionDto _boardDefinition;

        private readonly List<int> _turnOrder;
        private readonly Dictionary<int, PlayerRuntimeState> _playersByUserId;

        private readonly object _syncRoot = new object();
        private readonly Random _random;

        private readonly IBoardNavigator _boardNavigator;
        private readonly IDiceManager _diceService;
        private readonly IItemEffectManager _itemEffectService;

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
                .Where(id => id != INVALID_USER_ID)
                .ToList();

            if (_turnOrder.Count == 0)
            {
                throw new InvalidOperationException("GameplayLogic requires at least one player.");
            }

            _playersByUserId = _turnOrder
                .ToDictionary(
                    id => id,
                    id => new PlayerRuntimeState
                    {
                        UserId = id,
                        Position = MIN_CELL_INDEX,
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
            _diceService = new DiceManager(_random);
            _itemEffectService = new ItemEffectManager();

            _finalCellIndex = _boardNavigator.FinalCellIndex;
        }

        public TurnTimeoutResult HandleTurnTimeout()
        {
            lock (_syncRoot)
            {
                if (_isFinished)
                {
                    throw new InvalidOperationException("The game has already finished.");
                }

                if (_turnOrder.Count == 0)
                {
                    throw new InvalidOperationException("There are no players in this game.");
                }

                int previousTurnUserId = _turnOrder[_currentTurnIndex];

                if (!_playersByUserId.TryGetValue(previousTurnUserId, out PlayerRuntimeState currentPlayer))
                {
                    throw new InvalidOperationException("Current player state was not found.");
                }

                currentPlayer.ConsecutiveTimeouts++;

                bool playerKicked = false;
                int kickedUserId = INVALID_USER_ID;
                int winnerUserId = INVALID_USER_ID;

                if (currentPlayer.ConsecutiveTimeouts >= MAX_CONSECUTIVE_TIMEOUTS)
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
                            CurrentTurnUserId = INVALID_USER_ID,
                            PlayerKicked = true,
                            KickedUserId = kickedUserId,
                            GameFinished = true,
                            WinnerUserId = winnerUserId
                        };
                    }

                    if (_currentTurnIndex >= _turnOrder.Count)
                    {
                        _currentTurnIndex = _currentTurnIndex % _turnOrder.Count;
                    }
                }
                else
                {
                    AdvanceTurnAndResetFlags(previousTurnUserId);
                }

                int currentTurnUserId = _isFinished || _turnOrder.Count == 0
                    ? INVALID_USER_ID
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
                    throw new InvalidOperationException("The game has already finished.");
                }

                if (!_turnOrder.Contains(userId))
                {
                    throw new InvalidOperationException("User is not part of this game.");
                }

                int currentTurnUserId = _turnOrder[_currentTurnIndex];
                if (currentTurnUserId != userId)
                {
                    throw new InvalidOperationException("It is not this user's turn.");
                }

                if (!_playersByUserId.TryGetValue(userId, out PlayerRuntimeState playerState))
                {
                    throw new InvalidOperationException("Player state was not found.");
                }

                playerState.ConsecutiveTimeouts = 0;

                if (playerState.RemainingFrozenTurns > 0)
                {
                    playerState.RemainingFrozenTurns--;

                    if (playerState.RemainingFrozenTurns < 0)
                    {
                        playerState.RemainingFrozenTurns = 0;
                    }

                    string frozenExtraInfo = EXTRA_INFO_FROZEN_SKIP_TURN;

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

                playerState.HasRolledThisTurn = true;

                int diceValue = _diceService.GetDiceValue(playerState, diceCode);

                int fromCellIndex = playerState.Position;

                bool usedRocket = false;
                bool rocketIgnored = false;

                int tentativeTarget = fromCellIndex + diceValue;

                if (tentativeTarget < MIN_BOARD_CELL)
                {
                    tentativeTarget = MIN_BOARD_CELL;
                }

                if (playerState.PendingRocketBonus > 0)
                {
                    int tentativeTargetWithRocket = fromCellIndex
                        + diceValue
                        + playerState.PendingRocketBonus;

                    if (_finalCellIndex > 0 && tentativeTargetWithRocket <= _finalCellIndex)
                    {
                        tentativeTarget = tentativeTargetWithRocket;
                        usedRocket = true;
                    }
                    else
                    {
                        rocketIgnored = true;
                    }

                    playerState.PendingRocketBonus = 0;
                }

                if (_finalCellIndex > 0 && tentativeTarget > _finalCellIndex)
                {
                    string extraTooHighInfo = EXTRA_INFO_ROLL_TOO_HIGH_NO_MOVE;

                    if (rocketIgnored)
                    {
                        extraTooHighInfo = AppendToken(
                            EXTRA_INFO_ROCKET_IGNORED,
                            EXTRA_INFO_ROLL_TOO_HIGH_NO_MOVE);
                    }

                    AdvanceTurnAndResetFlags(userId);

                    return new RollDiceResult
                    {
                        DiceValue = diceValue,
                        FromCellIndex = fromCellIndex,
                        ToCellIndex = fromCellIndex,
                        IsGameOver = _isFinished,
                        ExtraInfo = extraTooHighInfo,
                        UsedRocket = false,
                        RocketIgnored = rocketIgnored,
                        MessageIndex = null,
                        GrantedItemCode = null,
                        GrantedDiceCode = null
                    };
                }

                int finalTarget = _boardNavigator.ApplyJumpEffectsIfAny(playerState, tentativeTarget);

                string extraInfo = string.Empty;

                if (TryGetJumpDestination(tentativeTarget, out int jumpDestination))
                {
                    bool isLadder = jumpDestination > tentativeTarget;
                    bool isSnake = jumpDestination < tentativeTarget;

                    if (isSnake && playerState.HasShield)
                    {
                        extraInfo = EXTRA_INFO_SNAKE_BLOCKED_BY_SHIELD;
                    }
                    else if (isLadder)
                    {
                        extraInfo = EXTRA_INFO_LADDER;
                    }
                    else if (isSnake)
                    {
                        extraInfo = EXTRA_INFO_SNAKE;
                    }
                    else
                    {
                        extraInfo = EXTRA_INFO_JUMP_SAME;
                    }
                }

                SpecialCellResult specialCellResult = _boardNavigator.ApplySpecialCellIfAny(
                    playerState,
                    finalTarget,
                    extraInfo);

                finalTarget = specialCellResult.FinalCellIndex;
                extraInfo = specialCellResult.ExtraInfo;

                int? messageIndex = specialCellResult.MessageIndex;
                bool shouldGrantExtraRoll = specialCellResult.GrantsExtraRoll;
                string grantedItemCode = specialCellResult.GrantedItemCode;
                string grantedDiceCode = specialCellResult.GrantedDiceCode;

                playerState.Position = finalTarget;

                bool isGameOver = false;

                if (_finalCellIndex > 0 && finalTarget >= _finalCellIndex)
                {
                    _isFinished = true;
                    isGameOver = true;

                    extraInfo = AppendToken(extraInfo, EXTRA_INFO_WIN);
                }

                string finalExtraInfo = extraInfo;

                if (usedRocket)
                {
                    finalExtraInfo = AppendToken(EXTRA_INFO_ROCKET_USED, finalExtraInfo);
                }
                else if (rocketIgnored)
                {
                    finalExtraInfo = AppendToken(EXTRA_INFO_ROCKET_IGNORED, finalExtraInfo);
                }

                bool hasExtraRoll = shouldGrantExtraRoll && !isGameOver;

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
                    ToCellIndex = finalTarget,
                    IsGameOver = isGameOver,
                    ExtraInfo = finalExtraInfo,
                    UsedRocket = usedRocket,
                    RocketIgnored = rocketIgnored,
                    MessageIndex = messageIndex,
                    GrantedItemCode = grantedItemCode,
                    GrantedDiceCode = grantedDiceCode
                };
            }
        }

        public GameStateSnapshot GetCurrentState()
        {
            lock (_syncRoot)
            {
                int currentTurnUserId = _turnOrder[_currentTurnIndex];

                var tokens = _playersByUserId
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
                            HasPendingRocketBonus = state.PendingRocketBonus > 0
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
                    throw new InvalidOperationException("La partida ya terminó.");
                }

                if (!_turnOrder.Contains(userId))
                {
                    throw new InvalidOperationException("El usuario no forma parte de esta partida.");
                }

                int currentTurnUserId = _turnOrder[_currentTurnIndex];
                if (currentTurnUserId != userId)
                {
                    throw new InvalidOperationException("No es el turno de este jugador.");
                }

                if (!_playersByUserId.TryGetValue(userId, out PlayerRuntimeState currentPlayer))
                {
                    throw new InvalidOperationException("Player state was not found.");
                }

                if (currentPlayer.RemainingFrozenTurns > 0)
                {
                    throw new InvalidOperationException("El jugador está congelado y no puede usar ítems.");
                }

                if (currentPlayer.HasRolledThisTurn)
                {
                    throw new InvalidOperationException("El jugador ya tiró el dado en este turno.");
                }

                if (currentPlayer.ItemUsedThisTurn)
                {
                    throw new InvalidOperationException("El jugador ya usó un ítem en este turno.");
                }

                ItemEffectResult result = _itemEffectService.UseItem(
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

        private int ApplyJumpEffectsIfAny(PlayerRuntimeState targetPlayer, int candidatePosition)
        {
            return _boardNavigator.ApplyJumpEffectsIfAny(targetPlayer, candidatePosition);
        }

        private void AdvanceTurnAndResetFlags(int currentUserId)
        {
            if (!_playersByUserId.TryGetValue(currentUserId, out PlayerRuntimeState currentPlayer))
            {
                return;
            }

            if (currentPlayer.HasShield && currentPlayer.RemainingShieldTurns > 0)
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

        private bool TryGetJumpDestination(int startIndex, out int destinationIndex)
        {
            // El BoardNavigator ya encapsula el mapa de jumps,
            // así que reutilizamos ApplyJumpEffectsIfAny comparando resultado.
            // Si no hay salto, resultado == startIndex.
            // Si hay, es diferente.
            var dummyPlayer = new PlayerRuntimeState
            {
                HasShield = false
            };

            int result = _boardNavigator.ApplyJumpEffectsIfAny(dummyPlayer, startIndex);

            if (result != startIndex)
            {
                destinationIndex = result;
                return true;
            }

            destinationIndex = startIndex;
            return false;
        }

        private static string AppendToken(string baseInfo, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return baseInfo ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(baseInfo))
            {
                return token;
            }

            return baseInfo + "_" + token;
        }

        private static void ResetPlayerForExtraRoll(PlayerRuntimeState playerState)
        {
            if (playerState == null)
            {
                return;
            }

            playerState.HasRolledThisTurn = false;
            playerState.ItemUsedThisTurn = false;
        }
    }
}
