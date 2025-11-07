using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class StatsRepository : IStatsRepository
    {
        private const int MAX_ALLOWED_RESULTS = 100;

        public IList<PlayerRankingItemDto> GetTopPlayersByCoins(int maxResults)
        {
            if (maxResults <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxResults),
                    "maxResults must be greater than zero."
                );
            }

            if (maxResults > MAX_ALLOWED_RESULTS)
            {
                maxResults = MAX_ALLOWED_RESULTS;
            }

            using (var context = new SnakeAndLaddersDBEntities1())
            {
                var query = context.Usuario
                    .OrderByDescending(u => u.Monedas)
                    .ThenBy(u => u.NombreUsuario)
                    .Select(u => new PlayerRankingItemDto
                    {
                        UserId = u.IdUsuario,
                        Username = u.NombreUsuario,
                        Coins = u.Monedas
                    });

                return query
                    .Take(maxResults)
                    .ToList();
            }
        }
    }
}
