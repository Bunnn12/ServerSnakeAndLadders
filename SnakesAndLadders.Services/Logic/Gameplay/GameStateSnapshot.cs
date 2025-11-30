using SnakeAndLadders.Contracts.Dtos.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class GameStateSnapshot
    {
        public int CurrentTurnUserId { get; set; }

        public bool IsFinished { get; set; }

        public IReadOnlyCollection<TokenStateDto> Tokens { get; set; }
    }
}
