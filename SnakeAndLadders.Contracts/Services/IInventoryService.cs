using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System.ServiceModel;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IInventoryService
    {
        [OperationContract]
        InventorySnapshotDto GetInventory(int userId);

        [OperationContract]
        void UpdateSelectedDice(int userId, DiceSlotsSelection selection);

        [OperationContract]
        void UpdateSelectedItems(int userId, ItemSlotsSelection selection);

        [OperationContract]
        void EquipItemToSlot(
            int userId,
            byte slotNumber,
            int objectId);

        [OperationContract]
        void UnequipItemFromSlot(
            int userId,
            byte slotNumber);

        [OperationContract]
        void EquipDiceToSlot(
            int userId,
            byte slotNumber,
            int diceId);

        [OperationContract]
        void UnequipDiceFromSlot(
            int userId,
            byte slotNumber);
    }
}
