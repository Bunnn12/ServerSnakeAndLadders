using System;
using System.Collections.Generic;

namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GameSession
    {
        /// <summary>
        /// Identificador de la partida (IdPartida).
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Definición del tablero usado en la partida.
        /// </summary>
        public BoardDefinitionDto Board { get; set; }

        /// <summary>
        /// Lista de IdUsuario de los jugadores que participaron en la partida.
        /// Incluye invitados y registrados.
        /// </summary>
        public IReadOnlyList<int> PlayerUserIds { get; set; }

        /// <summary>
        /// IdUsuario del jugador que tiene el turno actual.
        /// Será cero si ya no hay turno porque la partida terminó.
        /// </summary>
        public int CurrentTurnUserId { get; set; }

        /// <summary>
        /// Indica si la partida ya terminó.
        /// </summary>
        public bool IsFinished { get; set; }

        /// <summary>
        /// Indica si hay ganador registrado.
        /// </summary>
        public bool HasWinner { get; set; }

        /// <summary>
        /// IdUsuario del ganador.
        /// Será cero si HasWinner es false.
        /// </summary>
        public int WinnerUserId { get; set; }

        /// <summary>
        /// Nombre del ganador.
        /// Será cadena vacía si HasWinner es false.
        /// </summary>
        public string WinnerUserName { get; set; }

        /// <summary>
        /// Motivo de término de la partida (por ejemplo: "WIN", "ALL_LEFT", "TIMEOUT").
        /// Cadena vacía si la partida sigue en curso.
        /// </summary>
        public string EndReason { get; set; }

        /// <summary>
        /// Momento en que inició el turno actual (UTC).
        /// </summary>
        public DateTime CurrentTurnStartUtc { get; set; }

        /// <summary>
        /// Momento en que la partida terminó (UTC).
        /// DateTime.MinValue si todavía no termina.
        /// </summary>
        public DateTime FinishedAtUtc { get; set; }

        /// <summary>
        /// Indica si ya se aplicaron recompensas (monedas) a los usuarios registrados.
        /// </summary>
        public bool RewardsGranted { get; set; }

        public GameSession()
        {
            PlayerUserIds = Array.Empty<int>();
            WinnerUserName = string.Empty;
            EndReason = string.Empty;
            FinishedAtUtc = DateTime.MinValue;
        }
    }

    internal sealed class GameSessionPlayer
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public int CurrentCellIndex { get; set; }

        public int FinalCellIndex { get; set; }

        public int Rank { get; set; }

        public int CoinsDelta { get; set; }

        public bool IsWinner { get; set; }
    }

}
