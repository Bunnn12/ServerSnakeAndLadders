using ServerSnakesAndLadders.Common;
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
            UpdateItemSlotsRequest request);

        void UpdateSelectedDice(
            UpdateDiceSlotsRequest request);

        OperationResult<bool> GrantItemToUser(int userId, string itemCode);

        OperationResult<bool> GrantDiceToUser(int userId, string diceCode);
    }
}
