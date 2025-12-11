using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class UpdateDiceSlotsRequest
    {
        public int UserId { get; set; }

        public int? Slot1DiceId { get; set; }

        public int? Slot2DiceId { get; set; }
    }
}
