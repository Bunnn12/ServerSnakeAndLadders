using System;

namespace SnakeAndLadders.Contracts.Dtos
{
    /// <summary>
    /// Represents the statistics profile for a single player.
    /// </summary>
    public sealed class PlayerStatsDto
    {
        public int UserId { get; set; }

        public string Username { get; set; }

        public int MatchesPlayed { get; set; }

        public int MatchesWon { get; set; }

        /// <summary>
        /// Win percentage in the range [0,100], rounded to two decimals.
        /// </summary>
        public decimal WinPercentage { get; set; }

        /// <summary>
        /// Player position in the coins ranking if inside the
        /// configured top range (e.g. Top 50). Null otherwise.
        /// </summary>
        public int? RankingPosition { get; set; }

        public int Coins { get; set; }
    }
}
