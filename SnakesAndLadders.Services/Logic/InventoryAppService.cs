using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class InventoryAppService : IInventoryAppService
    {
        private const int MIN_VALID_USER_ID = 1;

        private const byte MIN_ITEM_SLOT = 1;
        private const byte MAX_ITEM_SLOT = 3;

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryAppService));

        private readonly IInventoryRepository inventoryRepository;

        public InventoryAppService(IInventoryRepository inventoryRepository)
        {
            this.inventoryRepository = inventoryRepository
                ?? throw new ArgumentNullException(nameof(inventoryRepository));
        }

        public InventorySnapshotDto GetInventory(int userId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            try
            {
                var items = inventoryRepository.GetUserItems(userId);
                var dice = inventoryRepository.GetUserDice(userId);

                return new InventorySnapshotDto
                {
                    Items = items == null
                        ? new List<InventoryItemDto>()
                        : new List<InventoryItemDto>(items),
                    Dice = dice == null
                        ? new List<InventoryDiceDto>()
                        : new List<InventoryDiceDto>(dice)
                };
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void UpdateSelectedItems(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            EnsureUniqueItemSelection(
                slot1ObjectId,
                slot2ObjectId,
                slot3ObjectId);

            try
            {
                inventoryRepository.UpdateSelectedItems(
                    userId,
                    slot1ObjectId,
                    slot2ObjectId,
                    slot3ObjectId);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            EnsureUniqueDiceSelection(
                slot1DiceId,
                slot2DiceId);

            try
            {
                inventoryRepository.UpdateSelectedDice(
                    userId,
                    slot1DiceId,
                    slot2DiceId);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void EquipItemToSlot(
            int userId,
            byte slotNumber,
            int objectId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidItemSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            if (objectId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(objectId));
            }

            try
            {
                var items = inventoryRepository.GetUserItems(userId)
                    ?? throw new InvalidOperationException("El usuario no tiene inventario de objetos.");

                var ownedItem = items.FirstOrDefault(i => i.ObjectId == objectId);

                if (ownedItem == null || ownedItem.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "El usuario no posee el objeto especificado o no tiene cantidad disponible.");
                }

                var slots = BuildCurrentItemSlots(items);

               
                foreach (var slotKey in slots.Keys.ToList())
                {
                    if (slots[slotKey].HasValue && slots[slotKey].Value == objectId)
                    {
                        slots[slotKey] = null;
                    }
                }

                slots[slotNumber] = objectId;

                EnsureUniqueItemSelection(
                    slots[MIN_ITEM_SLOT],
                    slots[(byte)(MIN_ITEM_SLOT + 1)],
                    slots[MAX_ITEM_SLOT]);

                inventoryRepository.UpdateSelectedItems(
                    userId,
                    slots[MIN_ITEM_SLOT],
                    slots[(byte)(MIN_ITEM_SLOT + 1)],
                    slots[MAX_ITEM_SLOT]);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void UnequipItemFromSlot(
            int userId,
            byte slotNumber)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidItemSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            try
            {
                inventoryRepository.RemoveItemFromSlot(userId, slotNumber);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void EquipDiceToSlot(
            int userId,
            byte slotNumber,
            int diceId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidDiceSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            if (diceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(diceId));
            }

            try
            {
                var diceList = inventoryRepository.GetUserDice(userId)
                    ?? throw new InvalidOperationException("El usuario no tiene inventario de dados.");

                var ownedDice = diceList.FirstOrDefault(d => d.DiceId == diceId);

                if (ownedDice == null || ownedDice.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "El usuario no posee el dado especificado o no tiene cantidad disponible.");
                }

                var slots = BuildCurrentDiceSlots(diceList);

                foreach (var slotKey in slots.Keys.ToList())
                {
                    if (slots[slotKey].HasValue && slots[slotKey].Value == diceId)
                    {
                        slots[slotKey] = null;
                    }
                }

                slots[slotNumber] = diceId;

                EnsureUniqueDiceSelection(
                    slots[MIN_DICE_SLOT],
                    slots[MAX_DICE_SLOT]);

                inventoryRepository.UpdateSelectedDice(
                    userId,
                    slots[MIN_DICE_SLOT],
                    slots[MAX_DICE_SLOT]);
            }
            catch (Exception )
            {
                throw;
            }
        }

        public void UnequipDiceFromSlot(
            int userId,
            byte slotNumber)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidDiceSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            try
            {
                inventoryRepository.RemoveDiceFromSlot(userId, slotNumber);
            }
            catch (Exception )
            {
                throw;
            }
        }

        private static bool IsValidUserId(int userId)
        {
            return userId >= MIN_VALID_USER_ID;
        }

        private static bool IsValidItemSlot(byte slotNumber)
        {
            return slotNumber >= MIN_ITEM_SLOT && slotNumber <= MAX_ITEM_SLOT;
        }

        private static bool IsValidDiceSlot(byte slotNumber)
        {
            return slotNumber >= MIN_DICE_SLOT && slotNumber <= MAX_DICE_SLOT;
        }

        private static void EnsureUniqueItemSelection(
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            var ids = new List<int>();

            if (slot1ObjectId.HasValue)
            {
                ids.Add(slot1ObjectId.Value);
            }

            if (slot2ObjectId.HasValue)
            {
                ids.Add(slot2ObjectId.Value);
            }

            if (slot3ObjectId.HasValue)
            {
                ids.Add(slot3ObjectId.Value);
            }

            EnsureNoDuplicates(ids, "objetos seleccionados");
        }

        private static void EnsureUniqueDiceSelection(
            int? slot1DiceId,
            int? slot2DiceId)
        {
            var ids = new List<int>();

            if (slot1DiceId.HasValue)
            {
                ids.Add(slot1DiceId.Value);
            }

            if (slot2DiceId.HasValue)
            {
                ids.Add(slot2DiceId.Value);
            }

            EnsureNoDuplicates(ids, "dados seleccionados");
        }

        private static void EnsureNoDuplicates(
            IReadOnlyCollection<int> ids,
            string contextDescription)
        {
            var seen = new HashSet<int>();

            foreach (int id in ids)
            {
                if (!seen.Add(id))
                {
                    throw new InvalidOperationException(
                        $"No se puede asignar el mismo identificador más de una vez en los {contextDescription}.");
                }
            }
        }

        private static Dictionary<byte, int?> BuildCurrentItemSlots(
            IEnumerable<InventoryItemDto> items)
        {
            var slots = new Dictionary<byte, int?>
            {
                { MIN_ITEM_SLOT, null },
                { (byte)(MIN_ITEM_SLOT + 1), null },
                { MAX_ITEM_SLOT, null }
            };

            foreach (InventoryItemDto item in items)
            {
                if (!item.SlotNumber.HasValue)
                {
                    continue;
                }

                byte slot = item.SlotNumber.Value;

                if (!IsValidItemSlot(slot))
                {
                    continue;
                }

                slots[slot] = item.ObjectId;
            }

            return slots;
        }

        private static Dictionary<byte, int?> BuildCurrentDiceSlots(
            IEnumerable<InventoryDiceDto> diceList)
        {
            var slots = new Dictionary<byte, int?>
            {
                { MIN_DICE_SLOT, null },
                { MAX_DICE_SLOT, null }
            };

            foreach (InventoryDiceDto dice in diceList)
            {
                if (!dice.SlotNumber.HasValue)
                {
                    continue;
                }

                byte slot = dice.SlotNumber.Value;

                if (!IsValidDiceSlot(slot))
                {
                    continue;
                }

                slots[slot] = dice.DiceId;
            }

            return slots;
        }
    }
}
