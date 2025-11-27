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
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId);

        void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId);
    }
}
