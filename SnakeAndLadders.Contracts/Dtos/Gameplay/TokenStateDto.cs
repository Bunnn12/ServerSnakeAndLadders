using System;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class TokenStateDto
    {
        public int UserId { get; set; }

        public int CellIndex { get; set; }
    }
}
