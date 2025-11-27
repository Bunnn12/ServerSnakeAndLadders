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
            int? slot1ObjectId,
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
    }
}
