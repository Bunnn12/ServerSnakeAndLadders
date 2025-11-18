using System;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GetGameStateResponseDto
    {
        public int GameId { get; set; }

        public int CurrentTurnUserId { get; set; }

        public bool IsFinished { get; set; }

        public List<TokenStateDto> Tokens { get; set; } = new List<TokenStateDto>();
    }
}
