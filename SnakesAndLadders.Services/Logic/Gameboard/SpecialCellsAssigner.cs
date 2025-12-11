using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic.Gameboard
{
    public sealed class SpecialCellsAssigner
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SpecialCellsAssigner));

        private readonly Random _random;
        private readonly object _randomLock = new object();

        public SpecialCellsAssigner()
        {
            _random = new Random();
        }

        public IReadOnlyCollection<SpecialCellType> GetEnabledSpecialTypes(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var enabled = new List<SpecialCellType>();

            if (request.EnableDiceCells)
            {
                enabled.Add(SpecialCellType.Dice);
            }

            if (request.EnableItemCells)
            {
                enabled.Add(SpecialCellType.Item);
            }

            if (request.EnableMessageCells)
            {
                enabled.Add(SpecialCellType.Message);
            }

            return enabled;
        }

        public void AssignSpecialCells(
            IList<BoardCellDto> cells,
            BoardSizeOption boardSize,
            IReadOnlyCollection<SpecialCellType> enabledTypes)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (enabledTypes == null || enabledTypes.Count == 0)
            {
                return;
            }

            int enabledTypesCount = enabledTypes.Count;
            int totalSpecial = GetTotalSpecialCells(boardSize, enabledTypesCount);

            if (totalSpecial <= 0)
            {
                return;
            }

            int lastIndex = cells.Count;

            var candidates = cells
                .Where(c =>
                    c.Index > GameBoardBuilderConstants.MIN_CELL_INDEX &&
                    c.Index < lastIndex &&
                    c.SpecialType == SpecialCellType.None)
                .ToList();

            if (candidates.Count == 0)
            {
                Logger.Warn(GameBoardBuilderConstants.LOG_WARN_NO_SPECIAL_CANDIDATES);
                return;
            }

            if (candidates.Count < totalSpecial)
            {
                totalSpecial = candidates.Count;
            }

            var specialTypesSequence = BuildSpecialTypesSequence(enabledTypes, totalSpecial);
            int totalRows = GetTotalRows(cells);
            int[] zoneTargets = CalculateZoneTargets(totalSpecial);
            int[] zoneCounts = new int[GameBoardBuilderConstants.ZONE_COUNT];

            var context = new SpecialCellPlacementContext(
                candidates,
                new List<BoardCellDto>(),
                zoneTargets,
                zoneCounts,
                totalRows);

            foreach (SpecialCellType specialType in specialTypesSequence)
            {
                PlaceSpecialCell(specialType, context);
            }

            int diceCount = cells.Count(c => c.SpecialType == SpecialCellType.Dice);
            int itemCount = cells.Count(c => c.SpecialType == SpecialCellType.Item);
            int messageCount = cells.Count(c => c.SpecialType == SpecialCellType.Message);

            Logger.InfoFormat(
                GameBoardBuilderConstants.LOG_INFO_SPECIAL_CELLS_ASSIGNED,
                diceCount,
                itemCount,
                messageCount);
        }

        private static int GetTotalRows(IEnumerable<BoardCellDto> cells)
        {
            return cells.Max(c => c.Row) + 1;
        }

        private static IList<SpecialCellType> BuildSpecialTypesSequence(
            IEnumerable<SpecialCellType> enabledTypes,
            int totalSpecial)
        {
            var result = new List<SpecialCellType>(totalSpecial);
            var enabledList = enabledTypes.ToList();
            int enabledCount = enabledList.Count;

            int perType = totalSpecial / enabledCount;
            int remainder = totalSpecial % enabledCount;

            foreach (SpecialCellType type in enabledList)
            {
                for (int i = 0; i < perType; i++)
                {
                    result.Add(type);
                }

                if (remainder > 0)
                {
                    result.Add(type);
                    remainder--;
                }
            }

            return result;
        }

        private static int[] CalculateZoneTargets(int totalSpecial)
        {
            var zoneTargets = new int[GameBoardBuilderConstants.ZONE_COUNT];

            int basePerZone = totalSpecial / GameBoardBuilderConstants.ZONE_COUNT;
            int remainder = totalSpecial % GameBoardBuilderConstants.ZONE_COUNT;

            for (int i = 0; i < GameBoardBuilderConstants.ZONE_COUNT; i++)
            {
                zoneTargets[i] = basePerZone;

                if (remainder > 0)
                {
                    zoneTargets[i]++;
                    remainder--;
                }
            }

            return zoneTargets;
        }

        private void PlaceSpecialCell(
            SpecialCellType specialType,
            SpecialCellPlacementContext context)
        {
            bool placed = TryPlaceWithRandomAttempts(specialType, context);

            if (!placed)
            {
                placed = TryPlaceFallbackNonAdjacent(specialType, context);
            }

            if (!placed)
            {
                TryPlaceFallbackAny(specialType, context);
            }
        }

        private bool TryPlaceWithRandomAttempts(
            SpecialCellType specialType,
            SpecialCellPlacementContext context)
        {
            for (int attempt = 0;
                 attempt < GameBoardBuilderConstants.MAX_ATTEMPTS_PER_SPECIAL_CELL
                 && context.Candidates.Count > 0;
                 attempt++)
            {
                int randomIndex;

                lock (_randomLock)
                {
                    randomIndex = _random.Next(0, context.Candidates.Count);
                }

                BoardCellDto candidate = context.Candidates[randomIndex];
                int zoneIndex = GetZoneIndex(candidate, context.TotalRows);

                if (IsAdjacentToAny(candidate, context.SelectedCells))
                {
                    continue;
                }

                if (context.ZoneCounts[zoneIndex] >= context.ZoneTargets[zoneIndex])
                {
                    continue;
                }

                AssignSpecialType(
                    specialType,
                    context,
                    randomIndex,
                    zoneIndex);

                return true;
            }

            return false;
        }

        private bool TryPlaceFallbackNonAdjacent(
            SpecialCellType specialType,
            SpecialCellPlacementContext context)
        {
            BoardCellDto fallback = context.Candidates
                .FirstOrDefault(c => !IsAdjacentToAny(c, context.SelectedCells));

            if (fallback == null)
            {
                return false;
            }

            int fallbackIndex = context.Candidates.IndexOf(fallback);
            int zoneIndex = GetZoneIndex(fallback, context.TotalRows);

            AssignSpecialType(
                specialType,
                context,
                fallbackIndex,
                zoneIndex);

            return true;
        }

        private bool TryPlaceFallbackAny(
            SpecialCellType specialType,
            SpecialCellPlacementContext context)
        {
            if (context.Candidates.Count == 0)
            {
                return false;
            }

            int candidateIndex = 0;
            BoardCellDto fallback = context.Candidates[candidateIndex];
            int zoneIndex = GetZoneIndex(fallback, context.TotalRows);

            AssignSpecialType(
                specialType,
                context,
                candidateIndex,
                zoneIndex);

            return true;
        }

        private static void AssignSpecialType(
            SpecialCellType specialType,
            SpecialCellPlacementContext context,
            int candidateIndex,
            int zoneIndex)
        {
            BoardCellDto cell = context.Candidates[candidateIndex];

            cell.SpecialType = specialType;
            context.SelectedCells.Add(cell);
            context.ZoneCounts[zoneIndex]++;
            context.Candidates.RemoveAt(candidateIndex);
        }

        private static int GetZoneIndex(BoardCellDto cell, int totalRows)
        {
            if (totalRows <= 0)
            {
                return 1;
            }

            int rowFromTop = cell.Row;
            int zone = (rowFromTop * GameBoardBuilderConstants.ZONE_COUNT) / totalRows;

            if (zone < 0)
            {
                zone = 0;
            }

            if (zone >= GameBoardBuilderConstants.ZONE_COUNT)
            {
                zone = GameBoardBuilderConstants.ZONE_COUNT - 1;
            }

            return zone;
        }

        private static bool IsAdjacentToAny(BoardCellDto candidate, IList<BoardCellDto> selected)
        {
            if (selected == null || selected.Count == 0)
            {
                return false;
            }

            return selected.Any(cell => AreAdjacent(cell, candidate));
        }

        private static bool AreAdjacent(BoardCellDto a, BoardCellDto b)
        {
            int deltaRow = Math.Abs(a.Row - b.Row);
            int deltaColumn = Math.Abs(a.Column - b.Column);

            return deltaRow <= 1 && deltaColumn <= 1;
        }

        private static int GetTotalSpecialCells(BoardSizeOption boardSize, int enabledTypesCount)
        {
            if (enabledTypesCount <= 0)
            {
                return 0;
            }

            switch (boardSize)
            {
                case BoardSizeOption.EightByEight:
                    return GetSpecialCellsFor8(enabledTypesCount);

                case BoardSizeOption.TenByTen:
                    return GetSpecialCellsFor10(enabledTypesCount);

                case BoardSizeOption.TwelveByTwelve:
                    return GetSpecialCellsFor12(enabledTypesCount);

                default:
                    return 0;
            }
        }

        private static int GetSpecialCellsFor8(int enabledTypesCount)
        {
            switch (enabledTypesCount)
            {
                case 1:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_8_ONE_TYPE;
                case 2:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_8_TWO_TYPES;
                case 3:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_8_THREE_TYPES;
                default:
                    return 0;
            }
        }

        private static int GetSpecialCellsFor10(int enabledTypesCount)
        {
            switch (enabledTypesCount)
            {
                case 1:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_10_ONE_TYPE;
                case 2:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_10_TWO_TYPES;
                case 3:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_10_THREE_TYPES;
                default:
                    return 0;
            }
        }

        private static int GetSpecialCellsFor12(int enabledTypesCount)
        {
            switch (enabledTypesCount)
            {
                case 1:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_12_ONE_TYPE;
                case 2:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_12_TWO_TYPES;
                case 3:
                    return GameBoardBuilderConstants.SPECIAL_CELLS_12_THREE_TYPES;
                default:
                    return 0;
            }
        }

        private sealed class SpecialCellPlacementContext
        {
            public SpecialCellPlacementContext(
                IList<BoardCellDto> candidates,
                IList<BoardCellDto> selectedCells,
                int[] zoneTargets,
                int[] zoneCounts,
                int totalRows)
            {
                Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
                SelectedCells = selectedCells ?? throw new ArgumentNullException(nameof(selectedCells));
                ZoneTargets = zoneTargets ?? throw new ArgumentNullException(nameof(zoneTargets));
                ZoneCounts = zoneCounts ?? throw new ArgumentNullException(nameof(zoneCounts));
                TotalRows = totalRows;
            }

            public IList<BoardCellDto> Candidates { get; }

            public IList<BoardCellDto> SelectedCells { get; }

            public int[] ZoneTargets { get; }

            public int[] ZoneCounts { get; }

            public int TotalRows { get; }
        }
    }
}
