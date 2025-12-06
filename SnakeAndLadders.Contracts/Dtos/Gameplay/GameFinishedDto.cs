using System;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GameFinishedDto
    {
        public int GameId { get; set; }

        /// <summary>
        /// True si la partida terminó en empate.
        /// </summary>
        public bool IsDraw { get; set; }

        public int? WinnerUserId { get; set; }

        public string WinnerUserName { get; set; }

        /// <summary>
        /// Mensaje corto del motivo de fin: "WIN", "ALL_LEFT", "TIMEOUT", etc.
        /// </summary>
        public string EndReason { get; set; }

        public DateTime FinishedAtUtc { get; set; }

        /// <summary>
        /// Ranking final de los jugadores.
        /// </summary>
        public IList<GameFinishedPlayerDto> Players { get; set; }
            = new List<GameFinishedPlayerDto>();
    }
}
