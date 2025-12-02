using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class InventoryService : IInventoryService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryService));

        private readonly IInventoryAppService inventoryAppService;

        public InventoryService(IInventoryAppService inventoryAppService)
        {
            this.inventoryAppService = inventoryAppService
                ?? throw new ArgumentNullException(nameof(inventoryAppService));
        }

        public InventorySnapshotDto GetInventory(int userId)
        {
            try
            {
                return inventoryAppService.GetInventory(userId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.GetInventory.", ex);
                throw;
            }
        }

        public void UpdateSelectedItems(
            int userId,
            int? slot1ObjectId, //Cambiar a slot1Item, cambiar a array 
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            try
            {
                inventoryAppService.UpdateSelectedItems(
                    userId,
                    slot1ObjectId,
                    slot2ObjectId,
                    slot3ObjectId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.UpdateSelectedItems.", ex);
                throw;
            }
        }

        public void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId)
        {
            try
            {
                inventoryAppService.UpdateSelectedDice(
                    userId,
                    slot1DiceId,
                    slot2DiceId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.UpdateSelectedDice.", ex);
                throw;
            }
        }

        public void EquipItemToSlot(
            int userId,
            byte slotNumber,
            int objectId)
        {
            try
            {
                inventoryAppService.EquipItemToSlot(
                    userId,
                    slotNumber,
                    objectId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.EquipItemToSlot.", ex);
                throw;
            }
        }

        public void UnequipItemFromSlot(
            int userId,
            byte slotNumber)
        {
            try
            {
                inventoryAppService.UnequipItemFromSlot(
                    userId,
                    slotNumber);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.UnequipItemFromSlot.", ex);
                throw;
            }
        }

        public void EquipDiceToSlot(
            int userId,
            byte slotNumber,
            int diceId)
        {
            try
            {
                inventoryAppService.EquipDiceToSlot(
                    userId,
                    slotNumber,
                    diceId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.EquipDiceToSlot.", ex);
                throw;
            }
        }

        public void UnequipDiceFromSlot(
            int userId,
            byte slotNumber)
        {
            try
            {
                inventoryAppService.UnequipDiceFromSlot(
                    userId,
                    slotNumber);
            }
            catch (Exception ex)
            {
                Logger.Error("Error en InventoryService.UnequipDiceFromSlot.", ex);
                throw;
            }
        }
    }
}
