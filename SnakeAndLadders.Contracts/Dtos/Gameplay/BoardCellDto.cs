using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class BoardCellDto
    {
        public int Index { get; set; }

        public int Row { get; set; }

        public int Column { get; set; }

        public bool IsDark { get; set; }

        public SpecialCellType SpecialType { get; set; }

        public bool HasSpecial => SpecialType != SpecialCellType.None;

        public bool IsStart { get; set; }

        public bool IsFinal { get; set; }
    }
}
