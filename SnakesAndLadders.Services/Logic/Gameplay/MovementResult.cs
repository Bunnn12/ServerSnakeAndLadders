using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class MovementResult
    {
        public int FinalPosition { get; set; }

        public string ExtraInfo { get; set; }

        public int? MessageIndex { get; set; }

        public bool ShouldGrantExtraRoll { get; set; }

        public string GrantedItemCode { get; set; }

        public string GrantedDiceCode { get; set; }
    }
}
