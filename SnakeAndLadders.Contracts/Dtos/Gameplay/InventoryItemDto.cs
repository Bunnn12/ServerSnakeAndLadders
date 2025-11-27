using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class InventoryItemDto
    {
        [DataMember]
        public int ObjectId { get; set; }

        [DataMember]
        public string ObjectCode { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Quantity { get; set; }

        [DataMember]
        public byte? SlotNumber { get; set; }
    }
}
