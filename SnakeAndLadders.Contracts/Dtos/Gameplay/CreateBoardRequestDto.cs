using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class CreateBoardRequestDto
    {
        public int GameId { get; set; }

        public BoardSizeOption BoardSize { get; set; }

        public bool EnableDiceCells { get; set; }

        public bool EnableItemCells { get; set; }

        public bool EnableMessageCells { get; set; }

        [DataMember]
        public string Difficulty { get; set; }

        [DataMember]
        public int[] PlayerUserIds { get; set; }

    }
}
