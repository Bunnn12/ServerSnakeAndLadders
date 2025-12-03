using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IInventoryRepository
    {
        IList<InventoryItemDto> GetUserItems(int userId);

        IList<InventoryDiceDto> GetUserDice(int userId);

        void RemoveItemFromSlot(int userId, byte slotNumber);

        void RemoveDiceFromSlot(int userId, byte slotNumber);

        void ConsumeItem(int userId, int objectId);

        void ConsumeDice(int userId, int diceId);

        void UpdateSelectedItems(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId);

        void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId);
    }
}
