using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class BoardDefinitionDto
    {
        public BoardSizeOption BoardSize { get; set; }

        public int Rows { get; set; }

        public int Columns { get; set; }

        public IList<BoardCellDto> Cells { get; set; }
    }
}
