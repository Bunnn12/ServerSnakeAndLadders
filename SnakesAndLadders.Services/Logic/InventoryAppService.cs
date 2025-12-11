using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using static SnakesAndLadders.Services.Constants.InventoryAppServiceConstants;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class InventoryAppService : IInventoryAppService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryAppService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository
                ?? throw new ArgumentNullException(nameof(inventoryRepository));
        }

        public InventorySnapshotDto GetInventory(int userId)
        {
            InventoryValidationHelper.ValidateUserId(userId);

            IList<InventoryItemDto> userItems = _inventoryRepository.GetUserItems(userId)
                ?? new List<InventoryItemDto>();

            IList<InventoryDiceDto> userDice = _inventoryRepository.GetUserDice(userId)
                ?? new List<InventoryDiceDto>();

            return new InventorySnapshotDto
            {
                Items = new List<InventoryItemDto>(userItems),
                Dice = new List<InventoryDiceDto>(userDice)
            };
        }

        public void UpdateSelectedItems(
            int userId,
            ItemSlotsSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            InventoryValidationHelper.ValidateUserId(userId);

            IEnumerable<int> selectedIds = InventorySlotsHelper.CreateSelectedIds(
                selection.Slot1ObjectId,
                selection.Slot2ObjectId,
                selection.Slot3ObjectId);

            InventorySlotsHelper.EnsureNoDuplicates(
                selectedIds,
                CONTEXT_SELECTED_ITEMS);

            var request = new UpdateItemSlotsRequest
            {
                UserId = userId,
                Slot1ObjectId = selection.Slot1ObjectId,
                Slot2ObjectId = selection.Slot2ObjectId,
                Slot3ObjectId = selection.Slot3ObjectId
            };

            _inventoryRepository.UpdateSelectedItems(request);
        }

        public void UpdateSelectedDice(
            int userId,
            DiceSlotsSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            InventoryValidationHelper.ValidateUserId(userId);

            IEnumerable<int> selectedIds = InventorySlotsHelper.CreateSelectedIds(
                selection.Slot1DiceId,
                selection.Slot2DiceId);

            InventorySlotsHelper.EnsureNoDuplicates(
                selectedIds,
                CONTEXT_SELECTED_DICE);

            var request = new UpdateDiceSlotsRequest
            {
                UserId = userId,
                Slot1DiceId = selection.Slot1DiceId,
                Slot2DiceId = selection.Slot2DiceId
            };

            _inventoryRepository.UpdateSelectedDice(request);
        }

        public void EquipItemToSlot(
            int userId,
            byte slotNumber,
            int objectId)
        {
            InventoryValidationHelper.ValidateUserId(userId);
            InventoryValidationHelper.ValidateItemSlot(slotNumber);
            InventoryValidationHelper.ValidateObjectId(objectId);

            IList<InventoryItemDto> userItems = _inventoryRepository.GetUserItems(userId)
                ?? throw new InvalidOperationException(ERROR_USER_NO_ITEM_INVENTORY);

            InventoryItemDto ownedItem = userItems.FirstOrDefault(item => item.ObjectId == objectId);

            if (ownedItem == null || ownedItem.Quantity <= 0)
            {
                throw new InvalidOperationException(ERROR_USER_NO_ITEM_OR_QUANTITY);
            }

            Dictionary<byte, int?> itemSlots = InventorySlotsHelper.BuildCurrentItemSlots(userItems);

            InventorySlotsHelper.ReplaceSlotAssignment(
                itemSlots,
                slotNumber,
                objectId);

            IEnumerable<int> selectedIds = InventorySlotsHelper.CreateSelectedIds(
                itemSlots[MIN_ITEM_SLOT],
                itemSlots[(byte)(MIN_ITEM_SLOT + 1)],
                itemSlots[MAX_ITEM_SLOT]);

            InventorySlotsHelper.EnsureNoDuplicates(
                selectedIds,
                CONTEXT_SELECTED_ITEMS);

            var request = new UpdateItemSlotsRequest
            {
                UserId = userId,
                Slot1ObjectId = itemSlots[MIN_ITEM_SLOT],
                Slot2ObjectId = itemSlots[(byte)(MIN_ITEM_SLOT + 1)],
                Slot3ObjectId = itemSlots[MAX_ITEM_SLOT]
            };

            _inventoryRepository.UpdateSelectedItems(request);
        }

        public void UnequipItemFromSlot(
            int userId,
            byte slotNumber)
        {
            InventoryValidationHelper.ValidateUserId(userId);
            InventoryValidationHelper.ValidateItemSlot(slotNumber);

            _inventoryRepository.RemoveItemFromSlot(userId, slotNumber);
        }

        public void EquipDiceToSlot(
            int userId,
            byte slotNumber,
            int diceId)
        {
            InventoryValidationHelper.ValidateUserId(userId);
            InventoryValidationHelper.ValidateDiceSlot(slotNumber);
            InventoryValidationHelper.ValidateDiceId(diceId);

            IList<InventoryDiceDto> userDice = _inventoryRepository.GetUserDice(userId)
                ?? throw new InvalidOperationException(ERROR_USER_NO_DICE_INVENTORY);

            InventoryDiceDto ownedDice = userDice.FirstOrDefault(dice => dice.DiceId == diceId);

            if (ownedDice == null || ownedDice.Quantity <= 0)
            {
                throw new InvalidOperationException(ERROR_USER_NO_DICE_OR_QUANTITY);
            }

            Dictionary<byte, int?> diceSlots = InventorySlotsHelper.BuildCurrentDiceSlots(userDice);

            InventorySlotsHelper.ReplaceSlotAssignment(
                diceSlots,
                slotNumber,
                diceId);

            IEnumerable<int> selectedIds = InventorySlotsHelper.CreateSelectedIds(
                diceSlots[MIN_DICE_SLOT],
                diceSlots[MAX_DICE_SLOT]);

            InventorySlotsHelper.EnsureNoDuplicates(
                selectedIds,
                CONTEXT_SELECTED_DICE);

            var request = new UpdateDiceSlotsRequest
            {
                UserId = userId,
                Slot1DiceId = diceSlots[MIN_DICE_SLOT],
                Slot2DiceId = diceSlots[MAX_DICE_SLOT]
            };

            _inventoryRepository.UpdateSelectedDice(request);
        }

        public void UnequipDiceFromSlot(
            int userId,
            byte slotNumber)
        {
            InventoryValidationHelper.ValidateUserId(userId);
            InventoryValidationHelper.ValidateDiceSlot(slotNumber);

            _inventoryRepository.RemoveDiceFromSlot(userId, slotNumber);
        }
    }
}
