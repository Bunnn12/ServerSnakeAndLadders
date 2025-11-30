using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class RollDiceResult
    {
        public int DiceValue { get; set; }
        public int FromCellIndex { get; set; }
        public int ToCellIndex { get; set; }
        public bool IsGameOver { get; set; }
        public string ExtraInfo { get; set; }
        public bool UsedRocket { get; set; }
        public bool RocketIgnored { get; set; }
    }
}
