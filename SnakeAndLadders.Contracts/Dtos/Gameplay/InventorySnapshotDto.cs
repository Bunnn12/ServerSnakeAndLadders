using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class InventorySnapshotDto
    {
        [DataMember]
        public List<InventoryItemDto> Items { get; set; }

        [DataMember]
        public List<InventoryDiceDto> Dice { get; set; }
    }
}
