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
        void UpdateSelectedItems(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId);

        [OperationContract]
        void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId);
    }
}
