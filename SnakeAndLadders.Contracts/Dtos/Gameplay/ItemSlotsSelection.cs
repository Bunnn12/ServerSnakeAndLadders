using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class ItemSlotsSelection
    {
        public int? Slot1ObjectId { get; set; }
        public int? Slot2ObjectId { get; set; }
        public int? Slot3ObjectId { get; set; }
    }
}
