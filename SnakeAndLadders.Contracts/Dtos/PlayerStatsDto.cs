using System;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class PlayerStatsDto
    {
        public int UserId { get; set; }

        public string Username { get; set; }

        public int MatchesPlayed { get; set; }

        public int MatchesWon { get; set; }

        public decimal WinPercentage { get; set; }

        public int? RankingPosition { get; set; }

        public int Coins { get; set; }
    }
}
