using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class TurnTimerState
    {
        public int GameId { get; }

        /// <summary>
        /// Usuario al que le toca el turno actualmente.
        /// </summary>
        public int CurrentTurnUserId { get; set; }

        /// <summary>
        /// Segundos restantes para que termine el turno.
        /// </summary>
        public int RemainingSeconds { get; set; }

        /// <summary>
        /// Última vez que se actualizó este estado (opcional, por si lo quieres usar).
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; }

        public TurnTimerState(int gameId, int currentTurnUserId, int initialSeconds)
        {
            GameId = gameId;
            CurrentTurnUserId = currentTurnUserId;
            RemainingSeconds = initialSeconds;
            LastUpdatedUtc = DateTime.UtcNow;
        }
    }
 }
