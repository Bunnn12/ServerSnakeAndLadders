using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using System;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class InventoryAppService : IInventoryAppService
    {
        private const int MIN_VALID_USER_ID = 1;

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
                        ? new System.Collections.Generic.List<InventoryItemDto>()
                        : new System.Collections.Generic.List<InventoryItemDto>(items),
                    Dice = dice == null
                        ? new System.Collections.Generic.List<InventoryDiceDto>()
                        : new System.Collections.Generic.List<InventoryDiceDto>(dice)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Error al obtener el inventario del usuario.", ex);
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

            try
            {
                inventoryRepository.UpdateSelectedItems(
                    userId,
                    slot1ObjectId,
                    slot2ObjectId,
                    slot3ObjectId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al actualizar los objetos seleccionados del usuario.", ex);
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

            try
            {
                inventoryRepository.UpdateSelectedDice(
                    userId,
                    slot1DiceId,
                    slot2DiceId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al actualizar los dados seleccionados del usuario.", ex);
                throw;
            }
        }

        private static bool IsValidUserId(int userId)
        {
            return userId >= MIN_VALID_USER_ID;
        }
    }
}
