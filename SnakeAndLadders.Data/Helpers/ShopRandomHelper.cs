using System;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class ShopRandomHelper
    {
        private static readonly Random _random = new Random();

        public static int NextExclusive(int maxExclusive)
        {
            if (maxExclusive <= ShopRepositoryConstants.RANDOM_MIN_VALUE)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            lock (_random)
            {
                return _random.Next(
                    ShopRepositoryConstants.RANDOM_MIN_VALUE,
                    maxExclusive);
            }
        }

        public static int NextRange(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            lock (_random)
            {
                return _random.Next(minInclusive, maxExclusive);
            }
        }
    }
}
