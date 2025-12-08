using SnakeAndLadders.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal sealed class ItemEffectManager : IItemEffectManager
    {
        private const int MIN_BOARD_CELL = 1;

        private const int ROCKET_BONUS_STEPS = 5;
        private const int ANCHOR_BACKWARD_STEPS = 3;
        private const int FREEZE_TURNS = 2;
        private const int SHIELD_TURNS = 3;

        private const string ITEM_CODE_ROCKET = "IT_ROCKET";
        private const string ITEM_CODE_ANCHOR = "IT_ANCHOR";
        private const string ITEM_CODE_SWAP = "IT_SWAP";
        private const string ITEM_CODE_FREEZE = "IT_FREEZE";
        private const string ITEM_CODE_SHIELD = "IT_SHIELD";

        public ItemEffectResult UseItem(
            string itemCode,
            int userId,
            int? targetUserId,
            IDictionary<int, PlayerRuntimeState> playersByUserId,
            Func<PlayerRuntimeState, int, int> applyJumpEffects)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentNullException(nameof(itemCode));
            }

            if (playersByUserId == null)
            {
                throw new ArgumentNullException(nameof(playersByUserId));
            }

            if (!playersByUserId.TryGetValue(userId, out PlayerRuntimeState currentPlayer))
            {
                throw new InvalidOperationException("Player state was not found.");
            }

            string normalizedCode = itemCode.Trim().ToUpperInvariant();

            switch (normalizedCode)
            {
                case ITEM_CODE_ROCKET:
                    EnsureSelfTargetOnly("Rocket", userId, targetUserId);
                    return ApplyRocket(currentPlayer);

                case ITEM_CODE_ANCHOR:
                    return ApplyAnchor(currentPlayer, targetUserId, playersByUserId, applyJumpEffects);

                case ITEM_CODE_SWAP:
                    EnsureOtherPlayerTargetRequired("Intercambio", userId, targetUserId);
                    return ApplySwap(currentPlayer, targetUserId, playersByUserId);

                case ITEM_CODE_FREEZE:
                    EnsureOtherPlayerTargetRequired("Congelar", userId, targetUserId);
                    return ApplyFreeze(currentPlayer, targetUserId, playersByUserId);

                case ITEM_CODE_SHIELD:
                    EnsureSelfTargetOnly("Escudo", userId, targetUserId);
                    return ApplyShield(currentPlayer);

                default:
                    throw new InvalidOperationException("Código de ítem no soportado.");
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

        private static ItemEffectResult ApplyRocket(PlayerRuntimeState currentPlayer)
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

        private static ItemEffectResult ApplyAnchor(
            PlayerRuntimeState currentPlayer,
            int? targetUserId,
            IDictionary<int, PlayerRuntimeState> playersByUserId,
            Func<PlayerRuntimeState, int, int> applyJumpEffects)
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

            int finalPosition = applyJumpEffects != null
                ? applyJumpEffects(targetPlayer, candidatePosition)
                : candidatePosition;

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

        private static ItemEffectResult ApplySwap(
            PlayerRuntimeState currentPlayer,
            int? targetUserId,
            IDictionary<int, PlayerRuntimeState> playersByUserId)
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
                ShouldConsumeItemImmediately = !wasBlockedByShield
            };
        }

        private static ItemEffectResult ApplyFreeze(
            PlayerRuntimeState currentPlayer,
            int? targetUserId,
            IDictionary<int, PlayerRuntimeState> playersByUserId)
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

        private static ItemEffectResult ApplyShield(PlayerRuntimeState currentPlayer)
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

    }
}
