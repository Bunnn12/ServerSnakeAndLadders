using System.Runtime.Serialization;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    [DataContract]
    public sealed class InventoryDiceDto
    {
        [DataMember]
        public int DiceId { get; set; }

        [DataMember]
        public string DiceCode { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Quantity { get; set; }

        [DataMember]
        public byte? SlotNumber { get; set; }
    }
}
