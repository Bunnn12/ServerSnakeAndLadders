
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class GameplayLogic
    {
        private const int DICE_MIN_VALUE = 1;
        private const int DICE_MAX_VALUE = 6;
        private const int INVALID_USER_ID = 0;

        private const int ROCKET_BONUS_STEPS = 5;
        private const int ANCHOR_BACKWARD_STEPS = 3;
        private const int FREEZE_TURNS = 2;
        private const int SHIELD_TURNS = 3;

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

        private const string ITEM_CODE_ROCKET = "IT_ROCKET";
        private const string ITEM_CODE_ANCHOR = "IT_ANCHOR";
        private const string ITEM_CODE_SWAP = "IT_SWAP";
        private const string ITEM_CODE_FREEZE = "IT_FREEZE";
        private const string ITEM_CODE_SHIELD = "IT_SHIELD";

        private readonly List<int> turnOrder;
        private readonly Dictionary<int, PlayerRuntimeState> playersByUserId;
        private readonly Dictionary<int, int> jumpDestinationsByStartIndex;

        private readonly int finalCellIndex;

        private readonly object syncRoot = new object();
        private readonly Random random;

        private int currentTurnIndex;
        private bool isFinished;

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

            turnOrder = playerUserIds
                .Distinct()
                .Where(id => id != INVALID_USER_ID)
                .ToList();

            if (turnOrder.Count == 0)
            {
                throw new InvalidOperationException("GameplayLogic requires at least one player.");
            }

            playersByUserId = turnOrder
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
                        HasRolledThisTurn = false
                    });

            currentTurnIndex = 0;
            isFinished = false;

            random = new Random(unchecked((int)DateTime.UtcNow.Ticks));

            finalCellIndex = ResolveFinalCellIndex(board);
            jumpDestinationsByStartIndex = ResolveJumpMap(board);
        }

        private static int ResolveFinalCellIndex(BoardDefinitionDto board)
        {
            if (board == null || board.Cells == null || board.Cells.Count == 0)
            {
                return 0;
            }

            return board.Cells.Max(c => c.Index);
        }

        private static Dictionary<int, int> ResolveJumpMap(BoardDefinitionDto board)
        {
            var result = new Dictionary<int, int>();

            if (board == null || board.Links == null || board.Links.Count == 0)
            {
                return result;
            }

            foreach (BoardLinkDto link in board.Links)
            {
                int from = link.StartIndex;
                int to = link.EndIndex;

                if (from > 0 && to > 0 && from != to)
                {
                    result[from] = to;
                }
            }

            return result;
        }

        public RollDiceResult RollDice(int userId)
        {
            lock (syncRoot)
            {
                if (isFinished)
                {
                    throw new InvalidOperationException("The game has already finished.");
                }

                if (!turnOrder.Contains(userId))
                {
                    throw new InvalidOperationException("User is not part of this game.");
                }

                int currentTurnUserId = turnOrder[currentTurnIndex];
                if (currentTurnUserId != userId)
                {
                    throw new InvalidOperationException("It is not this user's turn.");
                }

                PlayerRuntimeState playerState = playersByUserId[userId];

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
                        IsGameOver = isFinished,
                        ExtraInfo = frozenExtraInfo,
                        UsedRocket = false,
                        RocketIgnored = false
                    };
                }

                playerState.HasRolledThisTurn = true;

                int diceValue = random.Next(DICE_MIN_VALUE, DICE_MAX_VALUE + 1);

                int fromCellIndex = playerState.Position;

                bool usedRocket = false;
                bool rocketIgnored = false;

                int tentativeTarget = fromCellIndex + diceValue;

                if (playerState.PendingRocketBonus > 0)
                {
                    int tentativeTargetWithRocket = fromCellIndex + diceValue + playerState.PendingRocketBonus;

                    if (finalCellIndex > 0 && tentativeTargetWithRocket <= finalCellIndex)
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

                if (finalCellIndex > 0 && tentativeTarget > finalCellIndex)
                {
                    string extraTooHighInfo = EXTRA_INFO_ROLL_TOO_HIGH_NO_MOVE;

                    if (rocketIgnored)
                    {
                        extraTooHighInfo = EXTRA_INFO_ROCKET_IGNORED + "_" + EXTRA_INFO_ROLL_TOO_HIGH_NO_MOVE;
                    }

                    AdvanceTurnAndResetFlags(userId);

                    return new RollDiceResult
                    {
                        DiceValue = diceValue,
                        FromCellIndex = fromCellIndex,
                        ToCellIndex = fromCellIndex,
                        IsGameOver = isFinished,
                        ExtraInfo = extraTooHighInfo,
                        UsedRocket = false,
                        RocketIgnored = rocketIgnored
                    };
                }

                int finalTarget = ApplyJumpEffectsIfAny(playerState, tentativeTarget);

                string extraInfo = string.Empty;

                if (jumpDestinationsByStartIndex.TryGetValue(tentativeTarget, out int jumpDestination))
                {
                    bool isLadder = jumpDestination > tentativeTarget;
                    bool isSnake = jumpDestination < tentativeTarget;

                    if (isSnake && playerState.HasShield)
                    {
                        // Aquí sabemos que la serpiente fue bloqueada
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

                playerState.Position = finalTarget;

                bool isGameOver = false;
                if (finalCellIndex > 0 && finalTarget >= finalCellIndex)
                {
                    isFinished = true;
                    isGameOver = true;

                    extraInfo = string.IsNullOrWhiteSpace(extraInfo)
                        ? EXTRA_INFO_WIN
                        : extraInfo + "_" + EXTRA_INFO_WIN;
                }

                string rocketInfoToken = string.Empty;

                if (usedRocket)
                {
                    rocketInfoToken = string.IsNullOrWhiteSpace(extraInfo)
                        ? EXTRA_INFO_ROCKET_USED
                        : EXTRA_INFO_ROCKET_USED + "_" + extraInfo;
                }
                else if (rocketIgnored)
                {
                    rocketInfoToken = string.IsNullOrWhiteSpace(extraInfo)
                        ? EXTRA_INFO_ROCKET_IGNORED
                        : EXTRA_INFO_ROCKET_IGNORED + "_" + extraInfo;
                }

                string finalExtraInfo = string.IsNullOrWhiteSpace(rocketInfoToken)
                    ? extraInfo
                    : rocketInfoToken;

                AdvanceTurnAndResetFlags(userId);

                return new RollDiceResult
                {
                    DiceValue = diceValue,
                    FromCellIndex = fromCellIndex,
                    ToCellIndex = finalTarget,
                    IsGameOver = isGameOver,
                    ExtraInfo = finalExtraInfo,
                    UsedRocket = usedRocket,
                    RocketIgnored = rocketIgnored
                };
            }
        }

        public GameStateSnapshot GetCurrentState()
        {
            lock (syncRoot)
            {
                int currentTurnUserId = turnOrder[currentTurnIndex];

                var tokens = playersByUserId
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
                    IsFinished = isFinished,
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

            lock (syncRoot)
            {
                if (isFinished)
                {
                    throw new InvalidOperationException("La partida ya terminó.");
                }

                if (!turnOrder.Contains(userId))
                {
                    throw new InvalidOperationException("El usuario no forma parte de esta partida.");
                }

                int currentTurnUserId = turnOrder[currentTurnIndex];
                if (currentTurnUserId != userId)
                {
                    throw new InvalidOperationException("No es el turno de este jugador.");
                }

                PlayerRuntimeState currentPlayer = playersByUserId[userId];

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

                string normalizedCode = itemCode.Trim().ToUpperInvariant();

                ItemEffectResult result;

                switch (normalizedCode)
                {
                    case ITEM_CODE_ROCKET:
                        
                        EnsureSelfTargetOnly("Rocket", userId, targetUserId);
                        result = ApplyRocket(currentPlayer);
                        break;

                    case ITEM_CODE_ANCHOR:
                        
                        result = ApplyAnchor(currentPlayer, targetUserId);
                        break;

                    case ITEM_CODE_SWAP:
                        
                        EnsureOtherPlayerTargetRequired("Intercambio", userId, targetUserId);
                        result = ApplySwap(currentPlayer, targetUserId);
                        break;

                    case ITEM_CODE_FREEZE:
                        
                        EnsureOtherPlayerTargetRequired("Congelar", userId, targetUserId);
                        result = ApplyFreeze(currentPlayer, targetUserId);
                        break;

                    case ITEM_CODE_SHIELD:
                        
                        EnsureSelfTargetOnly("Escudo", userId, targetUserId);
                        result = ApplyShield(currentPlayer);
                        break;

                    default:
                        throw new InvalidOperationException("Código de ítem no soportado.");
                }

                if (!result.WasBlockedByShield)
                {
                    currentPlayer.ItemUsedThisTurn = true;
                }

                return result;
            }
        }

        private static void EnsureSelfTargetOnly(
            string itemDisplayName,
            int userId,
            int? targetUserId)
        {
            if (targetUserId.HasValue && targetUserId.Value != userId)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "{0} solo se puede usar sobre tu propio jugador.",
                        itemDisplayName));
            }
        }

        private static void EnsureOtherPlayerTargetRequired(
            string itemDisplayName,
            int userId,
            int? targetUserId)
        {
            if (!targetUserId.HasValue || targetUserId.Value == userId)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "{0} debe usarse sobre otro jugador.",
                        itemDisplayName));
            }
        }



        private ItemEffectResult ApplyRocket(PlayerRuntimeState currentPlayer)
        {
            currentPlayer.PendingRocketBonus = ROCKET_BONUS_STEPS;

            return new ItemEffectResult
            {
                ItemCode = ITEM_CODE_ROCKET,
                EffectType = ItemEffectType.Rocket,
                UserId = currentPlayer.UserId,
                TargetUserId = null,
                FromCellIndex = currentPlayer.Position,
                ToCellIndex = null,
                WasBlockedByShield = false,
                TargetFrozen = false,
                ShieldActivated = false,
                ShouldConsumeItemImmediately = false
            };
        }

        private ItemEffectResult ApplyAnchor(PlayerRuntimeState currentPlayer, int? targetUserId)
        {
            int effectiveTargetUserId = targetUserId ?? currentPlayer.UserId;

            if (!playersByUserId.TryGetValue(effectiveTargetUserId, out PlayerRuntimeState targetPlayer))
            {
                throw new InvalidOperationException("Target player is not part of this game.");
            }

            if (targetPlayer.HasShield)
            {
                throw new InvalidOperationException("No puedes usar Ancla contra un jugador que tiene un escudo activo.");
            }

            int originalPosition = targetPlayer.Position;
            int minimumCellForAnchor = MIN_BOARD_CELL + ANCHOR_BACKWARD_STEPS;

            if (originalPosition <= minimumCellForAnchor)
            {
                throw new InvalidOperationException(
                    "Solo puedes usar Ancla cuando estás exactamente a 3 casillas de la casilla inicial.");
            }

            int candidatePosition = originalPosition - ANCHOR_BACKWARD_STEPS;

            if (candidatePosition < MIN_BOARD_CELL)
            {
                candidatePosition = MIN_BOARD_CELL;
            }

            int finalPosition = ApplyJumpEffectsIfAny(targetPlayer, candidatePosition);
            targetPlayer.Position = finalPosition;

            return new ItemEffectResult
            {
                ItemCode = ITEM_CODE_ANCHOR,
                EffectType = ItemEffectType.Anchor,
                UserId = currentPlayer.UserId,
                TargetUserId = effectiveTargetUserId,
                FromCellIndex = originalPosition,
                ToCellIndex = finalPosition,
                WasBlockedByShield = false,
                TargetFrozen = false,
                ShieldActivated = false,
                ShouldConsumeItemImmediately = true
            };
        }





        private ItemEffectResult ApplySwap(PlayerRuntimeState currentPlayer, int? targetUserId)
        {
            if (!targetUserId.HasValue)
            {
                throw new InvalidOperationException("Swap item requires a target player.");
            }

            if (targetUserId.Value == currentPlayer.UserId)
            {
                throw new InvalidOperationException("Swap item cannot target the same player.");
            }

            if (!playersByUserId.TryGetValue(targetUserId.Value, out PlayerRuntimeState targetPlayer))
            {
                throw new InvalidOperationException("Target player is not part of this game.");
            }

            bool wasBlockedByShield = targetPlayer.HasShield;

            int fromCurrent = currentPlayer.Position;
            int fromTarget = targetPlayer.Position;

            int toCurrent = fromCurrent;
            int toTarget = fromTarget;

            if (!wasBlockedByShield)
            {
                toCurrent = fromTarget;
                toTarget = fromCurrent;

                currentPlayer.Position = toCurrent;
                targetPlayer.Position = toTarget;
            }

            return new ItemEffectResult
            {
                ItemCode = ITEM_CODE_SWAP,
                EffectType = ItemEffectType.Swap,
                UserId = currentPlayer.UserId,
                TargetUserId = targetUserId.Value,
                FromCellIndex = fromCurrent,
                ToCellIndex = toCurrent,
                WasBlockedByShield = wasBlockedByShield,
                TargetFrozen = false,
                ShieldActivated = false,
                // ⬇️ no efecto = no consumir
                ShouldConsumeItemImmediately = !wasBlockedByShield
            };
        }

        private ItemEffectResult ApplyFreeze(PlayerRuntimeState currentPlayer, int? targetUserId)
        {
            if (!targetUserId.HasValue)
            {
                throw new InvalidOperationException("Freeze item requires a target player.");
            }

            if (targetUserId.Value == currentPlayer.UserId)
            {
                throw new InvalidOperationException("Freeze item cannot target the same player.");
            }

            if (!playersByUserId.TryGetValue(targetUserId.Value, out PlayerRuntimeState targetPlayer))
            {
                throw new InvalidOperationException("Target player is not part of this game.");
            }

            if (targetPlayer.HasShield)
            {
                throw new InvalidOperationException("No puedes congelar a un jugador que tiene un escudo activo.");
            }

            if (targetPlayer.RemainingFrozenTurns > 0)
            {
                throw new InvalidOperationException("Target player is already frozen.");
            }

            targetPlayer.RemainingFrozenTurns = FREEZE_TURNS;

            return new ItemEffectResult
            {
                ItemCode = ITEM_CODE_FREEZE,
                EffectType = ItemEffectType.Freeze,
                UserId = currentPlayer.UserId,
                TargetUserId = targetUserId.Value,
                FromCellIndex = targetPlayer.Position,
                ToCellIndex = targetPlayer.Position,
                WasBlockedByShield = false,
                TargetFrozen = true,
                ShieldActivated = false,
                ShouldConsumeItemImmediately = true
            };
        }




        private ItemEffectResult ApplyShield(PlayerRuntimeState currentPlayer)
        {
            if (currentPlayer.HasShield && currentPlayer.RemainingShieldTurns > 0)
            {
                throw new InvalidOperationException("The player already has an active shield.");
            }

            currentPlayer.HasShield = true;
            currentPlayer.RemainingShieldTurns = SHIELD_TURNS;

            return new ItemEffectResult
            {
                ItemCode = ITEM_CODE_SHIELD,
                EffectType = ItemEffectType.Shield,
                UserId = currentPlayer.UserId,
                TargetUserId = null,
                FromCellIndex = currentPlayer.Position,
                ToCellIndex = currentPlayer.Position,
                WasBlockedByShield = false,
                TargetFrozen = false,
                ShieldActivated = true,
                ShouldConsumeItemImmediately = true
            };
        }

        private int ApplyJumpEffectsIfAny(PlayerRuntimeState targetPlayer, int candidatePosition)
        {
            int finalPosition = candidatePosition;

            if (!jumpDestinationsByStartIndex.TryGetValue(candidatePosition, out int jumpDestination))
            {
                return finalPosition;
            }

            bool isSnake = jumpDestination < candidatePosition;

            if (isSnake && targetPlayer.HasShield)
            {
                // Serpiente bloqueada por escudo: no se mueve
                return candidatePosition;
            }

            // No hay escudo o es escalera: se aplica el salto normal
            finalPosition = jumpDestination;

            return finalPosition;
        }


        private void AdvanceTurnAndResetFlags(int currentUserId)
        {
            if (!playersByUserId.TryGetValue(currentUserId, out PlayerRuntimeState currentPlayer))
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

            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        }
    }
}
