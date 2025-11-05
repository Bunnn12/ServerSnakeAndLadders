using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class CreateBoardRequestDto
    {
        public int GameId { get; set; }

        public BoardSizeOption BoardSize { get; set; }

        public bool EnableBonusCells { get; set; }

        public bool EnableTrapCells { get; set; }

        public bool EnableTeleportCells { get; set; }
    }
}
