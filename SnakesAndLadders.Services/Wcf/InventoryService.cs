using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Wcf.Constants;
using System;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class InventoryService : IInventoryService
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(InventoryService));

        private readonly IInventoryAppService _inventoryAppService;

        public InventoryService(IInventoryAppService inventoryAppService)
        {
            _inventoryAppService = inventoryAppService
                ?? throw new ArgumentNullException(nameof(inventoryAppService));
        }

        public InventorySnapshotDto GetInventory(int userId)
        {
            return ExecuteSafe(
                InventoryServiceConstants.OP_GET_INVENTORY,
                () => _inventoryAppService.GetInventory(userId));
        }

        public void UpdateSelectedItems(int userId, ItemSlotsSelection selection)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_UPDATE_SELECTED_ITEMS,
                () => _inventoryAppService.UpdateSelectedItems(userId, selection));
        }

        public void UpdateSelectedDice(int userId, DiceSlotsSelection selection)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_UPDATE_SELECTED_DICE,
                () => _inventoryAppService.UpdateSelectedDice(userId, selection));
        }

        public void EquipItemToSlot(int userId, byte slotNumber, int objectId)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_EQUIP_ITEM_TO_SLOT,
                () => _inventoryAppService.EquipItemToSlot(userId, slotNumber, objectId));
        }

        public void UnequipItemFromSlot(int userId, byte slotNumber)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_UNEQUIP_ITEM_FROM_SLOT,
                () => _inventoryAppService.UnequipItemFromSlot(userId, slotNumber));
        }

        public void EquipDiceToSlot(int userId, byte slotNumber, int diceId)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_EQUIP_DICE_TO_SLOT,
                () => _inventoryAppService.EquipDiceToSlot(userId, slotNumber, diceId));
        }

        public void UnequipDiceFromSlot(int userId, byte slotNumber)
        {
            ExecuteSafe(
                InventoryServiceConstants.OP_UNEQUIP_DICE_FROM_SLOT,
                () => _inventoryAppService.UnequipDiceFromSlot(userId, slotNumber));
        }

        private static T ExecuteSafe<T>(string operationName, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    string.Format(
                        InventoryServiceConstants.ERROR_MESSAGE_TEMPLATE,
                        operationName),
                    ex);

                throw;
            }
        }

        private static void ExecuteSafe(string operationName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    string.Format(
                        InventoryServiceConstants.ERROR_MESSAGE_TEMPLATE,
                        operationName),
                    ex);

                throw;
            }
        }
    }
}
