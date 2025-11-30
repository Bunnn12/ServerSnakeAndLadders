using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal class PendingRocketUsage
    {
        public int GameId { get; set; }

        public int UserId { get; set; }

        public byte SlotNumber { get; set; }

        public int ObjectId { get; set; }

        public string ItemCode { get; set; }
    }
}
