using System;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GetGameStateRequestDto
    {
        public int GameId { get; set; }
    }
}
