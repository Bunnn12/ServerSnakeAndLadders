using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class BoardDefinitionDto
    {
        [DataMember]
        public BoardSizeOption BoardSize { get; set; }

        [DataMember]
        public int Rows { get; set; }

        [DataMember]
        public int Columns { get; set; }

        [DataMember]
        public IList<BoardCellDto> Cells { get; set; }

        [DataMember]
        public IList<BoardLinkDto> Links { get; set; } = new List<BoardLinkDto>();

    }
}
