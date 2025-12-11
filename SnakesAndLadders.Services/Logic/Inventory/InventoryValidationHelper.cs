using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using static SnakesAndLadders.Services.Constants.InventoryAppServiceConstants;

namespace SnakesAndLadders.Services.Logic.Inventory
{
    internal static class InventoryValidationHelper
    {
        public static void ValidateUserId(int userId)
        {
            if (userId < MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(userId),
                    ERROR_USER_ID_POSITIVE);
            }
        }

        public static void ValidateItemSlot(byte slotNumber)
        {
            if (!IsValidItemSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotNumber),
                    ERROR_ITEM_SLOT_RANGE);
            }
        }

        public static void ValidateDiceSlot(byte slotNumber)
        {
            if (!IsValidDiceSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotNumber),
                    ERROR_DICE_SLOT_RANGE);
            }
        }

        public static void ValidateObjectId(int objectId)
        {
            if (objectId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(objectId),
                    ERROR_OBJECT_ID_POSITIVE);
            }
        }

        public static void ValidateDiceId(int diceId)
        {
            if (diceId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diceId),
                    ERROR_DICE_ID_POSITIVE);
            }
        }

        public static bool IsValidItemSlot(byte slotNumber)
        {
            return slotNumber >= MIN_ITEM_SLOT && slotNumber <= MAX_ITEM_SLOT;
        }

        public static bool IsValidDiceSlot(byte slotNumber)
        {
            return slotNumber >= MIN_DICE_SLOT && slotNumber <= MAX_DICE_SLOT;
        }
    }

    internal static class InventorySlotsHelper
    {
        public static Dictionary<byte, int?> BuildCurrentItemSlots(
            IEnumerable<InventoryItemDto> items)
        {
            var slots = InitializeSlots(MIN_ITEM_SLOT, MAX_ITEM_SLOT);

            foreach (InventoryItemDto item in items)
            {
                if (!item.SlotNumber.HasValue)
                {
                    continue;
                }

                byte slotNumber = item.SlotNumber.Value;

                if (!InventoryValidationHelper.IsValidItemSlot(slotNumber))
                {
                    continue;
                }

                slots[slotNumber] = item.ObjectId;
            }

            return slots;
        }

        public static Dictionary<byte, int?> BuildCurrentDiceSlots(
            IEnumerable<InventoryDiceDto> diceList)
        {
            var slots = InitializeSlots(MIN_DICE_SLOT, MAX_DICE_SLOT);

            foreach (InventoryDiceDto dice in diceList)
            {
                if (!dice.SlotNumber.HasValue)
                {
                    continue;
                }

                byte slotNumber = dice.SlotNumber.Value;

                if (!InventoryValidationHelper.IsValidDiceSlot(slotNumber))
                {
                    continue;
                }

                slots[slotNumber] = dice.DiceId;
            }

            return slots;
        }

        public static void ReplaceSlotAssignment(
            IDictionary<byte, int?> slots,
            byte targetSlot,
            int newId)
        {
            foreach (byte slotKey in slots.Keys.ToList())
            {
                int? assignedId = slots[slotKey];

                if (assignedId.HasValue && assignedId.Value == newId)
                {
                    slots[slotKey] = null;
                }
            }

            slots[targetSlot] = newId;
        }

        public static IEnumerable<int> CreateSelectedIds(
            params int?[] slotIds)
        {
            return slotIds
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();
        }

        public static void EnsureNoDuplicates(
            IEnumerable<int> selectedIds,
            string contextDescription)
        {
            var uniqueIds = new HashSet<int>();

            foreach (int id in selectedIds)
            {
                if (!uniqueIds.Add(id))
                {
                    string message = string.Format(
                        ERROR_DUPLICATED_SELECTION_TEMPLATE,
                        contextDescription);

                    throw new InvalidOperationException(message);
                }
            }
        }

        private static Dictionary<byte, int?> InitializeSlots(
            byte minSlot,
            byte maxSlot)
        {
            var slots = new Dictionary<byte, int?>();

            for (byte slot = minSlot; slot <= maxSlot; slot++)
            {
                slots[slot] = null;
            }

            return slots;
        }
    }
}
