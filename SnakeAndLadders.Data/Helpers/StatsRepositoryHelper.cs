using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class StatsRepositoryHelper
    {
        internal static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                StatsRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }

        internal static void ValidateUserId(
            int userId,
            string paramName,
            string errorMessage)
        {
            if (userId < StatsRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(paramName, errorMessage);
            }
        }

        internal static int NormalizeMaxResults(int maxResults)
        {
            if (maxResults < StatsRepositoryConstants.MIN_RESULTS)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxResults),
                    StatsRepositoryConstants.ERROR_MAX_RESULTS_POSITIVE);
            }

            if (maxResults > StatsRepositoryConstants.MAX_ALLOWED_RESULTS)
            {
                return StatsRepositoryConstants.MAX_ALLOWED_RESULTS;
            }

            return maxResults;
        }

        internal static int NormalizeRankingMaxResults(int rankingMaxResults)
        {
            if (rankingMaxResults < StatsRepositoryConstants.MIN_RESULTS)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rankingMaxResults),
                    StatsRepositoryConstants.ERROR_RANKING_MAX_RESULTS_POSITIVE);
            }

            if (rankingMaxResults > StatsRepositoryConstants.MAX_ALLOWED_RESULTS)
            {
                return StatsRepositoryConstants.MAX_ALLOWED_RESULTS;
            }

            return rankingMaxResults;
        }

        internal static bool IsWinnerFlagSet(byte[] winnerFlag)
        {
            if (winnerFlag == null
                || winnerFlag.Length < StatsRepositoryConstants.WINNER_FLAG_MIN_LENGTH)
            {
                return false;
            }

            return winnerFlag[StatsRepositoryConstants.WINNER_FLAG_INDEX] ==
                   StatsRepositoryConstants.WINNER_FLAG;
        }

        internal static decimal CalculateWinPercentage(int matchesPlayed, int matchesWon)
        {
            if (matchesPlayed < StatsRepositoryConstants.MIN_RESULTS
                || matchesWon < StatsRepositoryConstants.MIN_RESULTS)
            {
                return StatsRepositoryConstants.DEFAULT_WIN_PERCENTAGE;
            }

            decimal winRatio = (decimal)matchesWon / matchesPlayed;
            decimal percentage = Math.Round(
                winRatio * StatsRepositoryConstants.WIN_PERCENTAGE_FACTOR,
                StatsRepositoryConstants.WIN_PERCENTAGE_DECIMALS);

            return percentage;
        }
    }
}
