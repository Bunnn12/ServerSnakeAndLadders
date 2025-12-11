using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IInventoryAppService
    {
        InventorySnapshotDto GetInventory(int userId);

        void UpdateSelectedItems(
            int userId,
            ItemSlotsSelection selection);

        void UpdateSelectedDice(
            int userId,
            DiceSlotsSelection selection);

        void EquipItemToSlot(
        int userId,
        byte slotNumber,
        int objectId);

        void UnequipItemFromSlot(
            int userId,
            byte slotNumber);

        void EquipDiceToSlot(
            int userId,
            byte slotNumber,
            int diceId);

        void UnequipDiceFromSlot(
            int userId,
            byte slotNumber);
    }
}
