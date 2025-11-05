using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class CreateBoardResponseDto
    {
        public BoardDefinitionDto Board { get; set; }
    }
}
