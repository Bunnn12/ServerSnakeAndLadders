using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace SnakesAndLadders.Services.Logic
{
    internal sealed class GameBoardBuilder
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardBuilder));

        private const int BOARD_SIZE_8_ROWS = 8;
        private const int BOARD_SIZE_8_COLUMNS = 8;

        private const int BOARD_SIZE_10_ROWS = 10;
        private const int BOARD_SIZE_10_COLUMNS = 10;

        private const int BOARD_SIZE_12_ROWS = 12;
        private const int BOARD_SIZE_12_COLUMNS = 12;

        private const int SPECIAL_CELLS_8_ONE_TYPE = 3;
        private const int SPECIAL_CELLS_8_TWO_TYPES = 4;
        private const int SPECIAL_CELLS_8_THREE_TYPES = 6;

        private const int SPECIAL_CELLS_10_ONE_TYPE = 4;
        private const int SPECIAL_CELLS_10_TWO_TYPES = 8;
        private const int SPECIAL_CELLS_10_THREE_TYPES = 12;

        private const int SPECIAL_CELLS_12_ONE_TYPE = 5;
        private const int SPECIAL_CELLS_12_TWO_TYPES = 12;
        private const int SPECIAL_CELLS_12_THREE_TYPES = 15;

        private const int COLOR_PATTERN_MODULO = 2;
        private const int MIN_CELL_INDEX = 1;

        private const int LADDERS_8 = 4;
        private const int LADDERS_10 = 5;
        private const int LADDERS_12 = 6;

        private const int SNAKES_8_EASY = 4;
        private const int SNAKES_8_MEDIUM = 5;
        private const int SNAKES_8_HARD = 20; //6

        private const int SNAKES_10_EASY = 5;
        private const int SNAKES_10_MEDIUM = 6;
        private const int SNAKES_10_HARD = 7;

        private const int SNAKES_12_EASY = 6;
        private const int SNAKES_12_MEDIUM = 7;
        private const int SNAKES_12_HARD = 8;


        private static readonly Random RandomGenerator = new Random();
        private static readonly object RandomLock = new object();

        public BoardDefinitionDto BuildBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                Logger.Error("ocurrio un error de null");
                throw new ArgumentNullException(nameof(request));
                
            }

            if (request.GameId <= 0)
            {
                Logger.Error("ocurrio un error de argumentos");
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "GameId must be greater than zero.");
            }

            var enabledTypes = GetEnabledSpecialTypes(request).ToList();

            var layout = GetBoardLayout(request.BoardSize);

            var cells = CreateCells(layout);

            AssignSpecialCells(cells, request.BoardSize, enabledTypes);

            var board = new BoardDefinitionDto
            {
                BoardSize = request.BoardSize,
                Rows = layout.Rows,
                Columns = layout.Columns,
                Cells = cells
            };

            AddSnakesAndLadders(cells, board, request.Difficulty);


            Logger.Info("tablero creado");
            return board;
        }
        private static void AddSnakesAndLadders(
            IList<BoardCellDto> cells,
            BoardDefinitionDto board,
            string difficulty)
        {
            if (cells == null || cells.Count == 0)
            {
                board.Links = new List<BoardLinkDto>();
                return;
            }

            int totalCells = cells.Count;

            int ladders = GetLadderCount(board.BoardSize);
            int snakes = GetSnakeCount(board.BoardSize, difficulty);

            
            var usedIndexes = new HashSet<int>();

            foreach (var cell in cells)
            {
                if (cell.Index == MIN_CELL_INDEX || cell.Index == totalCells)
                {
                    usedIndexes.Add(cell.Index);
                    continue;
                }

                if (cell.SpecialType != SpecialCellType.None)
                {
                    usedIndexes.Add(cell.Index);
                }
            }

            var links = new List<BoardLinkDto>();

            AddLadders(cells, ladders, links, usedIndexes, totalCells);
            AddSnakes(cells, snakes, links, usedIndexes, totalCells);

            board.Links = links;

            Logger.InfoFormat("Snakes and ladders added. Ladders={0}, Snakes={1}", ladders, snakes);
        }


        private static int GetLadderCount(BoardSizeOption size)
        {
            switch (size)
            {
                case BoardSizeOption.EightByEight:
                    return LADDERS_8;

                case BoardSizeOption.TenByTen:
                    return LADDERS_10;

                case BoardSizeOption.TwelveByTwelve:
                    return LADDERS_12;

                default:
                    return 3; 
            }
        }


        private static int GetSnakeCount(BoardSizeOption size, string difficulty)
        {
            
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                difficulty = "medium";
            }

            difficulty = difficulty.Trim().ToLowerInvariant();

            switch (size)
            {
                case BoardSizeOption.EightByEight:
                    if (difficulty == "easy")
                    {
                        return SNAKES_8_EASY;
                    }
                    else if (difficulty == "hard")
                    {
                        return SNAKES_8_HARD;
                    }
                    else
                    {
                        return SNAKES_8_MEDIUM;
                    }

                case BoardSizeOption.TenByTen:
                    if (difficulty == "easy")
                    {
                        return SNAKES_10_EASY;
                    }
                    else if (difficulty == "hard")
                    {
                        return SNAKES_10_HARD;
                    }
                    else
                    {
                        return SNAKES_10_MEDIUM;
                    }

                case BoardSizeOption.TwelveByTwelve:
                    if (difficulty == "easy")
                    {
                        return SNAKES_12_EASY;
                    }
                    else if (difficulty == "hard")
                    {
                        return SNAKES_12_HARD;
                    }
                    else
                    {
                        return SNAKES_12_MEDIUM;
                    }

                default:
                    return 4; 
            }
        }


        private static void AddLadders(
             IList<BoardCellDto> cells,
             int count,
             IList<BoardLinkDto> links,
             HashSet<int> usedIndexes,
             int totalCells)
        {
            if (count <= 0)
                return;

            if (usedIndexes == null)
                usedIndexes = new HashSet<int>();

            var cellByIndex = cells.ToDictionary(c => c.Index);

            int attempts = 0;

            while (links.Count(l => l.IsLadder) < count && attempts < 1000)
            {
                attempts++;

                int start;
                int end;

                lock (RandomLock)
                {
                    
                    start = RandomGenerator.Next(2, totalCells - 4);
                    end = RandomGenerator.Next(start + 2, totalCells);
                }

                if (start <= MIN_CELL_INDEX ||
                    end <= MIN_CELL_INDEX ||
                    start >= totalCells ||
                    end >= totalCells)
                {
                    continue;
                }

                if (usedIndexes.Contains(start) || usedIndexes.Contains(end))
                    continue;

                if (cellByIndex[start].SpecialType != SpecialCellType.None ||
                    cellByIndex[end].SpecialType != SpecialCellType.None)
                {
                    continue;
                }

                int startRow = cellByIndex[start].Row;
                int endRow = cellByIndex[end].Row;

                if (startRow == endRow)
                    continue;

                if (startRow <= endRow)
                    continue;

                if (end == totalCells)
                    continue;

                usedIndexes.Add(start);
                usedIndexes.Add(end);

                links.Add(new BoardLinkDto
                {
                    StartIndex = start,
                    EndIndex = end,
                    IsLadder = true
                });
            }
        }

        private static void AddSnakes(
            IList<BoardCellDto> cells,
            int count,
            IList<BoardLinkDto> links,
            HashSet<int> usedIndexes,
            int totalCells)

        {
            if (count <= 0)
            {
                return;
            }

            if (usedIndexes == null)
            {
                usedIndexes = new HashSet<int>();
            }

            var cellByIndex = cells.ToDictionary(c => c.Index);

            int attempts = 0;

            while (links.Count(l => !l.IsLadder) < count && attempts < 1000)
            {
                attempts++;

                int start;
                int end;

                lock (RandomLock)
                {
                    start = RandomGenerator.Next(5, totalCells - 1); 
                    end = RandomGenerator.Next(2, start - 3);      
                }

                if (start <= MIN_CELL_INDEX ||
                    end <= MIN_CELL_INDEX ||
                    start >= totalCells ||
                    end >= totalCells)
                {
                    continue;
                }

                if (usedIndexes.Contains(start) || usedIndexes.Contains(end))
                {
                    continue;
                }

                if (cellByIndex[start].SpecialType != SpecialCellType.None ||
                    cellByIndex[end].SpecialType != SpecialCellType.None)
                {
                    continue;
                }

                int startRow = cellByIndex[start].Row;
                int endRow = cellByIndex[end].Row;

                if (startRow == endRow)
                {
                    continue;
                }

                if (startRow >= endRow)
                {
                    continue;
                }

                if (start == totalCells || end == MIN_CELL_INDEX)
                {
                    continue;
                }

                usedIndexes.Add(start);
                usedIndexes.Add(end);

                links.Add(new BoardLinkDto
                {
                    StartIndex = start,
                    EndIndex = end,
                    IsLadder = false
                });
            }
        }

        private static BoardLayoutDefinition GetBoardLayout(BoardSizeOption boardSize)
        {
            switch (boardSize)
            {
                case BoardSizeOption.EightByEight:
                    return new BoardLayoutDefinition(BOARD_SIZE_8_ROWS, BOARD_SIZE_8_COLUMNS);

                case BoardSizeOption.TenByTen:
                    return new BoardLayoutDefinition(BOARD_SIZE_10_ROWS, BOARD_SIZE_10_COLUMNS);

                case BoardSizeOption.TwelveByTwelve:
                    return new BoardLayoutDefinition(BOARD_SIZE_12_ROWS, BOARD_SIZE_12_COLUMNS);

                default:
                    throw new ArgumentOutOfRangeException(nameof(boardSize), "Unsupported board size.");
            }
        }

        private static IList<BoardCellDto> CreateCells(BoardLayoutDefinition layout)
        {
            var cells = new List<BoardCellDto>(layout.CellCount);

            int currentIndex = MIN_CELL_INDEX;

            for (int row = layout.Rows - 1; row >= 0; row--)
            {
                int distanceFromBottom = layout.Rows - 1 - row;
                bool isLeftToRight = (distanceFromBottom % COLOR_PATTERN_MODULO) == 0;

                if (isLeftToRight)
                {
                    for (int column = 0; column < layout.Columns; column++)
                    {
                        int viewRow = distanceFromBottom;
                        int viewColumn = column;

                        bool isDark = ((viewRow + viewColumn) % COLOR_PATTERN_MODULO) == 0;

                        cells.Add(new BoardCellDto
                        {
                            Index = currentIndex,
                            Row = row,
                            Column = column,
                            IsDark = isDark,
                            SpecialType = SpecialCellType.None,
                            IsStart = (currentIndex == MIN_CELL_INDEX),          
                            IsFinal = (currentIndex == layout.CellCount)         
                        });

                        currentIndex++;
                    }
                }
                else
                {
                    for (int column = layout.Columns - 1; column >= 0; column--)
                    {
                        int viewRow = distanceFromBottom;
                        int viewColumn = layout.Columns - 1 - column;

                        bool isDark = ((viewRow + viewColumn) % COLOR_PATTERN_MODULO) == 0;

                        cells.Add(new BoardCellDto
                        {
                            Index = currentIndex,
                            Row = row,
                            Column = column,
                            IsDark = isDark,
                            SpecialType = SpecialCellType.None,
                            IsStart = (currentIndex == MIN_CELL_INDEX),          
                            IsFinal = (currentIndex == layout.CellCount)     
                        });

                        currentIndex++;
                    }
                }
            }

            return cells;
        }

        private static void AssignSpecialCells(
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
                    c.Index > MIN_CELL_INDEX &&
                    c.Index < lastIndex &&
                    c.SpecialType == SpecialCellType.None)
                .ToList();

            if (candidates.Count == 0)
            {
                Logger.Warn("No hay celdas candidatas para casillas especiales.");
                return;
            }

            if (candidates.Count < totalSpecial)
            {
                totalSpecial = candidates.Count;
            }

            var specialTypesSequence = new List<SpecialCellType>(totalSpecial);
            int perType = totalSpecial / enabledTypesCount;

            foreach (SpecialCellType specialType in enabledTypes)
            {
                for (int i = 0; i < perType; i++)
                {
                    specialTypesSequence.Add(specialType);
                }
            }

            int totalRows = GetRowsForBoardSize(boardSize);

            int[] zoneTargets = CalculateZoneTargets(totalSpecial);
            int[] zoneCounts = new int[3];

            var selectedCells = new List<BoardCellDto>();

            const int MAX_ATTEMPTS_PER_CELL = 100;

            foreach (SpecialCellType specialType in specialTypesSequence)
            {
                bool placed = false;

                for (int attempt = 0; attempt < MAX_ATTEMPTS_PER_CELL && candidates.Count > 0; attempt++)
                {
                    int randomIndex;

                    lock (RandomLock)
                    {
                        randomIndex = RandomGenerator.Next(0, candidates.Count);
                    }

                    BoardCellDto candidate = candidates[randomIndex];

                    int zoneIndex = GetZoneIndex(candidate, totalRows);

                    if (IsAdjacentToAny(candidate, selectedCells))
                    {
                        continue;
                    }

                    if (zoneCounts[zoneIndex] >= zoneTargets[zoneIndex])
                    {
                        continue;
                    }

                    candidate.SpecialType = specialType;
                    selectedCells.Add(candidate);
                    zoneCounts[zoneIndex]++;
                    candidates.RemoveAt(randomIndex);
                    placed = true;
                    break;
                }

                if (!placed && candidates.Count > 0)
                {
                    BoardCellDto fallback = candidates
                        .FirstOrDefault(c => !IsAdjacentToAny(c, selectedCells));

                    if (fallback != null)
                    {
                        int zoneIndex = GetZoneIndex(fallback, totalRows);

                        fallback.SpecialType = specialType;
                        selectedCells.Add(fallback);
                        zoneCounts[zoneIndex]++;
                        candidates.Remove(fallback);
                        placed = true;
                    }
                }

                if (!placed && candidates.Count > 0)
                {
                    BoardCellDto fallback = candidates[0];
                    int zoneIndex = GetZoneIndex(fallback, totalRows);

                    fallback.SpecialType = specialType;
                    selectedCells.Add(fallback);
                    zoneCounts[zoneIndex]++;
                    candidates.RemoveAt(0);
                }
            }

            int bonusCount = cells.Count(c => c.SpecialType == SpecialCellType.Bonus);
            int trapCount = cells.Count(c => c.SpecialType == SpecialCellType.Trap);
            int teleportCount = cells.Count(c => c.SpecialType == SpecialCellType.Teleport);

            Logger.InfoFormat(
                "Special cells assigned with validations. Bonus={0}, Trap={1}, Teleport={2}",
                bonusCount,
                trapCount,
                teleportCount);
        }

        private static int GetRowsForBoardSize(BoardSizeOption boardSize)
        {
            switch (boardSize)
            {
                case BoardSizeOption.EightByEight:
                    return BOARD_SIZE_8_ROWS;

                case BoardSizeOption.TenByTen:
                    return BOARD_SIZE_10_ROWS;

                case BoardSizeOption.TwelveByTwelve:
                    return BOARD_SIZE_12_ROWS;

                default:
                    throw new ArgumentOutOfRangeException(nameof(boardSize), "Unsupported board size.");
            }
        }

        private static int[] CalculateZoneTargets(int totalSpecial)
        {
            var zoneTargets = new int[3];

            int basePerZone = totalSpecial / 3;
            int remainder = totalSpecial % 3;

            for (int i = 0; i < 3; i++)
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

        private static int GetZoneIndex(BoardCellDto cell, int totalRows)
        {
            if (totalRows <= 0)
            {
                return 1; 
            }

            int rowFromTop = cell.Row;

            int zone = (rowFromTop * 3) / totalRows;

            if (zone < 0)
            {
                zone = 0;
            }

            if (zone > 2)
            {
                zone = 2;
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
                    switch (enabledTypesCount)
                    {
                        case 1:
                            return SPECIAL_CELLS_8_ONE_TYPE;
                        case 2:
                            return SPECIAL_CELLS_8_TWO_TYPES;
                        case 3:
                            return SPECIAL_CELLS_8_THREE_TYPES;
                        default:
                            return 0;
                    }

                case BoardSizeOption.TenByTen:
                    switch (enabledTypesCount)
                    {
                        case 1:
                            return SPECIAL_CELLS_10_ONE_TYPE;
                        case 2:
                            return SPECIAL_CELLS_10_TWO_TYPES;
                        case 3:
                            return SPECIAL_CELLS_10_THREE_TYPES;
                        default:
                            return 0;
                    }

                case BoardSizeOption.TwelveByTwelve:
                    switch (enabledTypesCount)
                    {
                        case 1:
                            return SPECIAL_CELLS_12_ONE_TYPE;
                        case 2:
                            return SPECIAL_CELLS_12_TWO_TYPES;
                        case 3:
                            return SPECIAL_CELLS_12_THREE_TYPES;
                        default:
                            return 0;
                    }

                default:
                    return 0;
            }
        }

        private static IEnumerable<SpecialCellType> GetEnabledSpecialTypes(CreateBoardRequestDto request)
        {
            if (request.EnableBonusCells)
            {
                yield return SpecialCellType.Bonus;
            }

            if (request.EnableTrapCells)
            {
                yield return SpecialCellType.Trap;
            }

            if (request.EnableTeleportCells)
            {
                yield return SpecialCellType.Teleport;
            }
        }

        private readonly struct BoardLayoutDefinition
        {
            public BoardLayoutDefinition(int rows, int columns)
            {
                Rows = rows;
                Columns = columns;
                CellCount = rows * columns;
            }

            public int Rows { get; }

            public int Columns { get; }

            public int CellCount { get; }
        }

    }
}
