using System;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GameSession
    {
        public int GameId { get; set; }

        public BoardDefinitionDto Board { get; set; }

        public IReadOnlyList<int> PlayerUserIds { get; set; }

        public int CurrentTurnUserId { get; set; }

        public bool IsFinished { get; set; }

        public int WinnerUserId { get; set; }

        public string EndReason { get; set; }

        public DateTime CurrentTurnStartUtc { get; set; }
    }
}
