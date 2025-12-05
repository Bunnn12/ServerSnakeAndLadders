using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public class TurnTimeoutResult
    {
        public int PreviousTurnUserId { get; set; }

        public int CurrentTurnUserId { get; set; }

        public bool PlayerKicked { get; set; }

        public int KickedUserId { get; set; }

        public bool GameFinished { get; set; }

        public int WinnerUserId { get; set; }
    }
}
