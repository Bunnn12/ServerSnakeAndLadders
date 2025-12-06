namespace SnakeAndLadders.Contracts.Dtos.Gameplay
{
    public sealed class GameFinishedPlayerDto
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        /// <summary>
        /// Posición final (1 = primero, 2 = segundo, etc.).
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Casilla final en el tablero (por si quieres mostrarla).
        /// </summary>
        public int FinalCellIndex { get; set; }

        /// <summary>
        /// Monedas ganadas o perdidas en la partida (puede ser 0 si aún no lo manejas).
        /// </summary>
        public int CoinsDelta { get; set; }

        /// <summary>
        /// True si es uno de los ganadores (soporta empates).
        /// </summary>
        public bool IsWinner { get; set; }
    }
}
