using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class DiceManager : IDiceManager
    {
        private const int DICE_MIN_VALUE = 1;
        private const int DICE_MAX_VALUE = 6;
        private const int NEGATIVE_DICE_MIN_POSITION = 7;

        private const string DICE_CODE_NEGATIVE = "DICE_NEG";
        private const string DICE_CODE_ONE_TWO_THREE = "DICE_123";
        private const string DICE_CODE_FOUR_FIVE_SIX = "DICE_456";

        private readonly Random _random;

        public DiceManager(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int GetDiceValue(PlayerRuntimeState playerState, string diceCode)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (string.IsNullOrWhiteSpace(diceCode))
            {
                return _random.Next(DICE_MIN_VALUE, DICE_MAX_VALUE + 1);
            }

            string normalizedCode = diceCode.Trim().ToUpperInvariant();

            if (normalizedCode == DICE_CODE_NEGATIVE)
            {
                if (playerState.Position < NEGATIVE_DICE_MIN_POSITION)
                {
                    throw new InvalidOperationException(
                        "No puedes usar el dado negativo tan cerca de la casilla inicial.");
                }

                int absoluteValue = _random.Next(DICE_MIN_VALUE, DICE_MAX_VALUE + 1);
                return -absoluteValue;
            }

            if (normalizedCode == DICE_CODE_ONE_TWO_THREE)
            {
                return _random.Next(1, 3 + 1);
            }

            if (normalizedCode == DICE_CODE_FOUR_FIVE_SIX)
            {
                return _random.Next(4, 6 + 1);
            }

            return _random.Next(DICE_MIN_VALUE, DICE_MAX_VALUE + 1);
        }
    }
}
