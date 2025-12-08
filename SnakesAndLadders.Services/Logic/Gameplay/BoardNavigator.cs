using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal sealed class BoardNavigator : IBoardNavigator
    {
        private const int MIN_BOARD_CELL = 1;

        private const int MESSAGE_MIN_INDEX = 1;
        private const int MESSAGE_MAX_INDEX = 20;
        private const int MESSAGE_EFFECT_THRESHOLD = 10;
        private const int MESSAGE_ADVANCE_STEPS = 4;
        private const int MESSAGE_BACKWARD_STEPS = 5;

        private const string EXTRA_INFO_MESSAGE_ADVANCE = "MSG_ADVANCE_4";
        private const string EXTRA_INFO_MESSAGE_BACKWARD = "MSG_BACK_5";
        private const string EXTRA_INFO_MESSAGE_REROLL = "MSG_REROLL";
        private const string EXTRA_INFO_MESSAGE_SKIP_TURN = "MSG_SKIP_NEXT";

        private const string EXTRA_INFO_ITEM_GRANTED = "ITEM_GRANTED";
        private const string EXTRA_INFO_DICE_GRANTED = "DICE_GRANTED";

        private const string ITEM_CODE_ROCKET = "IT_ROCKET";
        private const string ITEM_CODE_ANCHOR = "IT_ANCHOR";
        private const string ITEM_CODE_SWAP = "IT_SWAP";
        private const string ITEM_CODE_FREEZE = "IT_FREEZE";
        private const string ITEM_CODE_SHIELD = "IT_SHIELD";

        private const string DICE_CODE_NEGATIVE = "DICE_NEG";
        private const string DICE_CODE_ONE_TWO_THREE = "DICE_123";
        private const string DICE_CODE_FOUR_FIVE_SIX = "DICE_456";

        private static readonly string[] ItemCodesPool =
        {
            ITEM_CODE_ROCKET,
            ITEM_CODE_ANCHOR,
            ITEM_CODE_SWAP,
            ITEM_CODE_FREEZE,
            ITEM_CODE_SHIELD
        };

        private static readonly string[] DiceCodesPool =
        {
            DICE_CODE_NEGATIVE,
            DICE_CODE_ONE_TWO_THREE,
            DICE_CODE_FOUR_FIVE_SIX
        };

        private readonly BoardDefinitionDto _boardDefinition;
        private readonly Dictionary<int, int> _jumpDestinationsByStartIndex;
        private readonly Random _random;

        public int FinalCellIndex { get; }

        public BoardNavigator(
            BoardDefinitionDto boardDefinition,
            Random random)
        {
            _boardDefinition = boardDefinition ?? throw new ArgumentNullException(nameof(boardDefinition));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            FinalCellIndex = ResolveFinalCellIndex(_boardDefinition);
            _jumpDestinationsByStartIndex = ResolveJumpMap(_boardDefinition);
        }

        public int ApplyJumpEffectsIfAny(PlayerRuntimeState targetPlayer, int candidatePosition)
        {
            if (targetPlayer == null)
            {
                throw new ArgumentNullException(nameof(targetPlayer));
            }

            int finalPosition = candidatePosition;

            if (!_jumpDestinationsByStartIndex.TryGetValue(candidatePosition, out int jumpDestination))
            {
                return finalPosition;
            }

            bool isSnake = jumpDestination < candidatePosition;

            if (isSnake && targetPlayer.HasShield)
            {
                return candidatePosition;
            }

            finalPosition = jumpDestination;

            return finalPosition;
        }

        public SpecialCellResult ApplySpecialCellIfAny(
            PlayerRuntimeState playerState,
            int currentCellIndex,
            string currentExtraInfo)
        {
            var result = new SpecialCellResult
            {
                FinalCellIndex = currentCellIndex,
                ExtraInfo = currentExtraInfo ?? string.Empty
            };

            if (_boardDefinition == null ||
                _boardDefinition.Cells == null ||
                _boardDefinition.Cells.Count == 0)
            {
                return result;
            }

            BoardCellDto cell = _boardDefinition
                .Cells
                .FirstOrDefault(c => c.Index == currentCellIndex);

            if (cell == null || cell.SpecialType == SpecialCellType.None)
            {
                return result;
            }

            switch (cell.SpecialType)
            {
                case SpecialCellType.Message:
                    ApplyMessageCellEffect(
                        playerState,
                        ref result);
                    break;

                case SpecialCellType.Item:
                    ApplyItemCellEffect(ref result);
                    break;

                case SpecialCellType.Dice:
                    ApplyDiceCellEffect(ref result);
                    break;

                default:
                    break;
            }

            return result;
        }

        private static int ResolveFinalCellIndex(BoardDefinitionDto board)
        {
            if (board == null || board.Cells == null || board.Cells.Count == 0)
            {
                return 0;
            }

            return board.Cells.Max(c => c.Index);
        }

        private static Dictionary<int, int> ResolveJumpMap(BoardDefinitionDto board)
        {
            var result = new Dictionary<int, int>();

            if (board == null || board.Links == null || board.Links.Count == 0)
            {
                return result;
            }

            foreach (BoardLinkDto link in board.Links)
            {
                int from = link.StartIndex;
                int to = link.EndIndex;

                if (from > 0 && to > 0 && from != to)
                {
                    result[from] = to;
                }
            }

            return result;
        }

        private void ApplyMessageCellEffect(
            PlayerRuntimeState playerState,
            ref SpecialCellResult result)
        {
            if (playerState == null)
            {
                return;
            }

            int generatedIndex = _random.Next(
                MESSAGE_MIN_INDEX,
                MESSAGE_MAX_INDEX + 1);

            result.MessageIndex = generatedIndex;

            bool isEven = (generatedIndex % 2) == 0;
            bool isSmallOrEqual = generatedIndex <= MESSAGE_EFFECT_THRESHOLD;

            if (isEven && isSmallOrEqual)
            {
                int candidate = result.FinalCellIndex + MESSAGE_ADVANCE_STEPS;

                if (FinalCellIndex > 0 && candidate > FinalCellIndex)
                {
                    candidate = FinalCellIndex;
                }

                if (candidate < MIN_BOARD_CELL)
                {
                    candidate = MIN_BOARD_CELL;
                }

                result.FinalCellIndex = candidate;
                result.ExtraInfo = AppendToken(result.ExtraInfo, EXTRA_INFO_MESSAGE_ADVANCE);

                ApplyPostMessageMovementEffects(playerState, ref result);
            }
            else if (isEven && !isSmallOrEqual)
            {
                result.GrantsExtraRoll = true;
                result.ExtraInfo = AppendToken(result.ExtraInfo, EXTRA_INFO_MESSAGE_REROLL);
            }
            else if (!isEven && isSmallOrEqual)
            {
                int candidate = result.FinalCellIndex - MESSAGE_BACKWARD_STEPS;

                if (candidate < MIN_BOARD_CELL)
                {
                    candidate = MIN_BOARD_CELL;
                }

                result.FinalCellIndex = candidate;
                result.ExtraInfo = AppendToken(result.ExtraInfo, EXTRA_INFO_MESSAGE_BACKWARD);

                ApplyPostMessageMovementEffects(playerState, ref result);
            }
            else
            {
                playerState.RemainingFrozenTurns++;
                result.ExtraInfo = AppendToken(result.ExtraInfo, EXTRA_INFO_MESSAGE_SKIP_TURN);
            }
        }

        private void ApplyPostMessageMovementEffects(
            PlayerRuntimeState playerState,
            ref SpecialCellResult result)
        {
            result.GrantedItemCode = null;
            result.GrantedDiceCode = null;

            result.FinalCellIndex = ApplyJumpEffectsIfAny(playerState, result.FinalCellIndex);

            if (_boardDefinition == null ||
                _boardDefinition.Cells == null ||
                _boardDefinition.Cells.Count == 0)
            {
                return;
            }

            int targetIndex = result.FinalCellIndex;

            BoardCellDto cell = _boardDefinition
                .Cells
                .FirstOrDefault(c => c.Index == targetIndex);

            if (cell == null || cell.SpecialType == SpecialCellType.None)
            {
                return;
            }

            switch (cell.SpecialType)
            {
                case SpecialCellType.Item:
                    ApplyItemCellEffect(ref result);
                    break;

                case SpecialCellType.Dice:
                    ApplyDiceCellEffect(ref result);
                    break;

                case SpecialCellType.Message:
                default:
                    // No encadenamos otro mensaje en el mismo turno.
                    break;
            }
        }

        private void ApplyItemCellEffect(ref SpecialCellResult result)
        {
            if (ItemCodesPool.Length == 0)
            {
                result.GrantedItemCode = null;
                return;
            }

            int index = _random.Next(0, ItemCodesPool.Length);
            string grantedItemCode = ItemCodesPool[index];

            result.GrantedItemCode = grantedItemCode;
            result.ExtraInfo = AppendToken(
                result.ExtraInfo,
                EXTRA_INFO_ITEM_GRANTED + "_" + grantedItemCode);
        }

        private void ApplyDiceCellEffect(ref SpecialCellResult result)
        {
            if (DiceCodesPool.Length == 0)
            {
                result.GrantedDiceCode = null;
                return;
            }

            int index = _random.Next(0, DiceCodesPool.Length);
            string grantedDiceCode = DiceCodesPool[index];

            result.GrantedDiceCode = grantedDiceCode;
            result.ExtraInfo = AppendToken(
                result.ExtraInfo,
                EXTRA_INFO_DICE_GRANTED + "_" + grantedDiceCode);
        }

        private static string AppendToken(string baseInfo, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return baseInfo ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(baseInfo))
            {
                return token;
            }

            return baseInfo + "_" + token;
        }
    }
}
