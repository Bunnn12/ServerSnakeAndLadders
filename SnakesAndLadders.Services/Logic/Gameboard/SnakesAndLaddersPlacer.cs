using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameboard
{
    public sealed class SnakesAndLaddersPlacer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SnakesAndLaddersPlacer));

        private readonly Random _random;
        private readonly object _randomLock = new object();

        public SnakesAndLaddersPlacer()
        {
            _random = new Random();
        }

        public void AddSnakesAndLadders(
            IList<BoardCellDto> cells,
            BoardDefinitionDto board,
            string difficulty)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (cells.Count == 0)
            {
                board.Links = new List<BoardLinkDto>();
                return;
            }

            int totalCells = cells.Count;
            int ladders = GetLadderCount(board.BoardSize);
            int snakes = GetSnakeCount(board.BoardSize, difficulty);

            var usedIndexes = BuildUsedIndexes(cells, totalCells);
            var links = new List<BoardLinkDto>();
            var context = new SnakeLadderContext(cells, links, usedIndexes, totalCells);

            AddLadders(context, ladders);
            AddSnakes(context, snakes);

            board.Links = links;

            Logger.InfoFormat(
                GameBoardBuilderConstants.LOG_INFO_SNAKES_LADDERS_ADDED,
                ladders,
                snakes);
        }

        private static HashSet<int> BuildUsedIndexes(IList<BoardCellDto> cells, int totalCells)
        {
            var usedIndexes = new HashSet<int>();

            foreach (BoardCellDto cell in cells)
            {
                if (cell.Index == GameBoardBuilderConstants.MIN_CELL_INDEX || cell.Index == totalCells)
                {
                    usedIndexes.Add(cell.Index);
                    continue;
                }

                if (cell.SpecialType != SpecialCellType.None)
                {
                    usedIndexes.Add(cell.Index);
                }
            }

            return usedIndexes;
        }

        private static int GetLadderCount(BoardSizeOption size)
        {
            switch (size)
            {
                case BoardSizeOption.EightByEight:
                    return GameBoardBuilderConstants.LADDERS_8;

                case BoardSizeOption.TenByTen:
                    return GameBoardBuilderConstants.LADDERS_10;

                case BoardSizeOption.TwelveByTwelve:
                    return GameBoardBuilderConstants.LADDERS_12;

                default:
                    return GameBoardBuilderConstants.DEFAULT_LADDERS_FALLBACK;
            }
        }

        private static int GetSnakeCount(BoardSizeOption size, string difficulty)
        {
            string effectiveDifficulty = NormalizeDifficulty(difficulty);

            switch (size)
            {
                case BoardSizeOption.EightByEight:
                    return GetSnakesFor8(effectiveDifficulty);

                case BoardSizeOption.TenByTen:
                    return GetSnakesFor10(effectiveDifficulty);

                case BoardSizeOption.TwelveByTwelve:
                    return GetSnakesFor12(effectiveDifficulty);

                default:
                    return GameBoardBuilderConstants.DEFAULT_SNAKES_FALLBACK;
            }
        }

        private static string NormalizeDifficulty(string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                return GameBoardBuilderConstants.DIFFICULTY_MEDIUM;
            }

            return difficulty.Trim().ToLowerInvariant();
        }

        private static int GetSnakesFor8(string difficulty)
        {
            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_EASY)
            {
                return GameBoardBuilderConstants.SNAKES_8_EASY;
            }

            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_HARD)
            {
                return GameBoardBuilderConstants.SNAKES_8_HARD;
            }

            return GameBoardBuilderConstants.SNAKES_8_MEDIUM;
        }

        private static int GetSnakesFor10(string difficulty)
        {
            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_EASY)
            {
                return GameBoardBuilderConstants.SNAKES_10_EASY;
            }

            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_HARD)
            {
                return GameBoardBuilderConstants.SNAKES_10_HARD;
            }

            return GameBoardBuilderConstants.SNAKES_10_MEDIUM;
        }

        private static int GetSnakesFor12(string difficulty)
        {
            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_EASY)
            {
                return GameBoardBuilderConstants.SNAKES_12_EASY;
            }

            if (difficulty == GameBoardBuilderConstants.DIFFICULTY_HARD)
            {
                return GameBoardBuilderConstants.SNAKES_12_HARD;
            }

            return GameBoardBuilderConstants.SNAKES_12_MEDIUM;
        }

        private void AddLadders(SnakeLadderContext context, int ladderCount)
        {
            if (ladderCount <= 0)
            {
                return;
            }

            int attempts = 0;

            while (context.Links.Count(l => l.IsLadder) < ladderCount
                   && attempts < GameBoardBuilderConstants.MAX_LADDER_PLACEMENT_ATTEMPTS)
            {
                attempts++;

                int startIndex;
                int endIndex;

                lock (_randomLock)
                {
                    int maxStart = context.TotalCells - GameBoardBuilderConstants.LADDERS_START_MAX_OFFSET;
                    startIndex = _random.Next(
                        GameBoardBuilderConstants.LADDERS_START_MIN_INDEX,
                        maxStart);

                    endIndex = _random.Next(
                        startIndex + GameBoardBuilderConstants.LADDERS_MIN_DISTANCE,
                        context.TotalCells);
                }

                if (!IsValidLadderCandidate(context, startIndex, endIndex))
                {
                    continue;
                }

                BoardCellDto startCell = context.CellByIndex[startIndex];
                BoardCellDto endCell = context.CellByIndex[endIndex];

                if (!IsValidLadderGeometry(context, startCell, endCell))
                {
                    continue;
                }

                context.UsedIndexes.Add(startIndex);
                context.UsedIndexes.Add(endIndex);

                context.Links.Add(new BoardLinkDto
                {
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    IsLadder = true
                });
            }
        }

        private void AddSnakes(SnakeLadderContext context, int snakeCount)
        {
            if (snakeCount <= 0)
            {
                return;
            }

            int attempts = 0;

            while (context.Links.Count(l => !l.IsLadder) < snakeCount
                   && attempts < GameBoardBuilderConstants.MAX_SNAKE_PLACEMENT_ATTEMPTS)
            {
                attempts++;

                int startIndex;
                int endIndex;

                lock (_randomLock)
                {
                    startIndex = _random.Next(
                        GameBoardBuilderConstants.SNAKES_START_MIN_INDEX,
                        context.TotalCells - 1);

                    endIndex = _random.Next(
                        GameBoardBuilderConstants.SNAKES_END_MIN_INDEX,
                        startIndex - GameBoardBuilderConstants.SNAKES_MIN_DISTANCE);
                }

                if (!IsValidSnakeCandidate(context, startIndex, endIndex))
                {
                    continue;
                }

                BoardCellDto startCell = context.CellByIndex[startIndex];
                BoardCellDto endCell = context.CellByIndex[endIndex];

                if (!IsValidSnakeGeometry(context, startCell, endCell))
                {
                    continue;
                }

                context.UsedIndexes.Add(startIndex);
                context.UsedIndexes.Add(endIndex);

                context.Links.Add(new BoardLinkDto
                {
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    IsLadder = false
                });
            }
        }

        private static bool IsValidLadderCandidate(SnakeLadderContext context, int startIndex, int endIndex)
        {
            if (!IsWithinPlayableRange(context.TotalCells, startIndex, endIndex))
            {
                return false;
            }

            if (context.UsedIndexes.Contains(startIndex) || context.UsedIndexes.Contains(endIndex))
            {
                return false;
            }

            if (context.CellByIndex[startIndex].SpecialType != SpecialCellType.None
                || context.CellByIndex[endIndex].SpecialType != SpecialCellType.None)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidSnakeCandidate(SnakeLadderContext context, int startIndex, int endIndex)
        {
            if (!IsWithinPlayableRange(context.TotalCells, startIndex, endIndex))
            {
                return false;
            }

            if (context.UsedIndexes.Contains(startIndex) || context.UsedIndexes.Contains(endIndex))
            {
                return false;
            }

            if (context.CellByIndex[startIndex].SpecialType != SpecialCellType.None
                || context.CellByIndex[endIndex].SpecialType != SpecialCellType.None)
            {
                return false;
            }

            return true;
        }

        private static bool IsWithinPlayableRange(int totalCells, int startIndex, int endIndex)
        {
            if (startIndex <= GameBoardBuilderConstants.MIN_CELL_INDEX
                || endIndex <= GameBoardBuilderConstants.MIN_CELL_INDEX)
            {
                return false;
            }

            if (startIndex >= totalCells
                || endIndex >= totalCells)
            {
                return false;
            }

            if (endIndex == totalCells)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidLadderGeometry(SnakeLadderContext context, BoardCellDto startCell, BoardCellDto endCell)
        {
            if (startCell.Row == endCell.Row)
            {
                return false;
            }

            if (startCell.Row <= endCell.Row)
            {
                return false;
            }

            if (endCell.Index == context.TotalCells)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidSnakeGeometry(SnakeLadderContext context, BoardCellDto startCell, BoardCellDto endCell)
        {
            if (startCell.Row == endCell.Row)
            {
                return false;
            }

            if (startCell.Row >= endCell.Row)
            {
                return false;
            }

            if (startCell.Index == context.TotalCells
                || endCell.Index == GameBoardBuilderConstants.MIN_CELL_INDEX)
            {
                return false;
            }

            return true;
        }

        
    }
}
