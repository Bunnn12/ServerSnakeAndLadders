using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Repository.Hierarchy;

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

        private const int LADDERS_8 = 3;
        private const int LADDERS_10 = 4;
        private const int LADDERS_12 = 5;

        private const int SNAKES_8_EASY = 3;
        private const int SNAKES_8_MEDIUM = 4;
        private const int SNAKES_8_HARD = 5;

        private const int SNAKES_10_EASY = 4;
        private const int SNAKES_10_MEDIUM = 5;
        private const int SNAKES_10_HARD = 6;

        private const int SNAKES_12_EASY = 5;
        private const int SNAKES_12_MEDIUM = 6;
        private const int SNAKES_12_HARD = 7;


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
                throw new ArgumentOutOfRangeException(nameof(request.GameId), "GameId must be greater than zero.");
                
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
            int totalCells = cells.Count;
            int rows = board.Rows;

            int ladders = GetLadderCount(board.BoardSize);
            int snakes = GetSnakeCount(board.BoardSize, difficulty);

            var usedIndexes = new HashSet<int>(
                cells.Where(c => c.SpecialType != SpecialCellType.None)
                     .Select(c => c.Index));

            usedIndexes.Add(1);
            usedIndexes.Add(totalCells);

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
            // Normalizamos el texto por seguridad
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
                    return 4; // fallback general
            }
        }


        private static void AddLadders(
    IList<BoardCellDto> cells,
    int count,
    IList<BoardLinkDto> links,
    HashSet<int> usedIndexes,
    int totalCells)
        {
            int attempts = 0;

            while (links.Count(l => l.IsLadder) < count && attempts < 500)
            {
                attempts++;

                int start;
                int end;

                lock (RandomLock)
                {
                    // Escalera: empieza en una celda segura (no primera ni última ni penúltima)
                    start = RandomGenerator.Next(2, totalCells - 4);
                    end = RandomGenerator.Next(start + 2, totalCells - 1);
                }

                // 1) No usar ni la primera ni la última celda
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

                if (cells[start - 1].SpecialType != SpecialCellType.None ||
                    cells[end - 1].SpecialType != SpecialCellType.None)
                {
                    continue;
                }

                int startRow = cells[start - 1].Row;
                int endRow = cells[end - 1].Row;

                // 2) No permitir escaleras horizontales
                if (startRow == endRow)
                {
                    continue;
                }

                // 3) Asegurar que la escalera suba visualmente
                if (startRow <= endRow)
                {
                    continue;
                }

                // 4) No permitir que termine en la última fila
                if (end == totalCells)
                {
                    continue;
                }

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
            int attempts = 0;

            while (links.Count(l => !l.IsLadder) < count && attempts < 500)
            {
                attempts++;

                int start;
                int end;

                lock (RandomLock)
                {
                    // Serpiente: empieza más arriba, termina más abajo, sin tocar bordes
                    start = RandomGenerator.Next(5, totalCells - 1);
                    end = RandomGenerator.Next(2, start - 3);
                }

                // 1) No usar ni la primera ni la última celda
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

                if (cells[start - 1].SpecialType != SpecialCellType.None ||
                    cells[end - 1].SpecialType != SpecialCellType.None)
                {
                    continue;
                }

                int startRow = cells[start - 1].Row;
                int endRow = cells[end - 1].Row;

                // 2) No permitir serpientes horizontales
                if (startRow == endRow)
                {
                    continue;
                }

                // 3) Asegurar que la serpiente baje visualmente
                if (startRow >= endRow)
                {
                    continue;
                }

                // 4) No permitir que empiece en la primera fila ni termine en la última
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

            int currentIndex = 1;

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
                            SpecialType = SpecialCellType.None
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
                            SpecialType = SpecialCellType.None
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

            // 1) Distribución por tipo (mantiene tus cantidades: 2+2+2, 4+4+4, etc.)
            var specialTypesSequence = new List<SpecialCellType>(totalSpecial);
            int perType = totalSpecial / enabledTypesCount;

            foreach (SpecialCellType specialType in enabledTypes)
            {
                for (int i = 0; i < perType; i++)
                {
                    specialTypesSequence.Add(specialType);
                }
            }

            // 2) Info de filas y zonas (arriba / medio / abajo)
            int totalRows = GetRowsForBoardSize(boardSize);

            int[] zoneTargets = CalculateZoneTargets(totalSpecial);
            int[] zoneCounts = new int[3];

            var selectedCells = new List<BoardCellDto>();

            const int MAX_ATTEMPTS_PER_CELL = 100;

            foreach (SpecialCellType specialType in specialTypesSequence)
            {
                bool placed = false;

                // Intentamos respetar: no adyacente + cupo de zona
                for (int attempt = 0; attempt < MAX_ATTEMPTS_PER_CELL && candidates.Count > 0; attempt++)
                {
                    int randomIndex;

                    lock (RandomLock)
                    {
                        randomIndex = RandomGenerator.Next(0, candidates.Count);
                    }

                    BoardCellDto candidate = candidates[randomIndex];

                    int zoneIndex = GetZoneIndex(candidate, totalRows);

                    // Validación 1: no adyacente a otra casilla especial ya colocada
                    if (IsAdjacentToAny(candidate, selectedCells))
                    {
                        continue;
                    }

                    // Validación 2: no pasar del cupo planeado en esa zona
                    if (zoneCounts[zoneIndex] >= zoneTargets[zoneIndex])
                    {
                        continue;
                    }

                    // OK, esta celda cumple las reglas
                    candidate.SpecialType = specialType;
                    selectedCells.Add(candidate);
                    zoneCounts[zoneIndex]++;
                    candidates.RemoveAt(randomIndex);
                    placed = true;
                    break;
                }

                // Fallback 1: si no encontramos respetando la zona, ignoramos zona
                // pero seguimos evitando adyacencia.
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

                // Fallback 2: si ni así se puede, colocamos donde se pueda
                // (para no perder la cantidad total de especiales).
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

        /// <summary>
        /// Divide el tablero en 3 zonas (arriba, medio, abajo)
        /// y calcula cuántas casillas especiales debería tener cada zona
        /// para que queden lo más equilibradas posible.
        /// </summary>
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

        /// <summary>
        /// Devuelve el índice de zona (0 = arriba, 1 = medio, 2 = abajo)
        /// en función de la fila de la celda.
        /// </summary>
        private static int GetZoneIndex(BoardCellDto cell, int totalRows)
        {
            if (totalRows <= 0)
            {
                return 1; // medio por defecto
            }

            // Row va de 0 (arriba) a totalRows - 1 (abajo).
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

        /// <summary>
        /// Indica si la celda candidata es adyacente (incluye diagonales)
        /// a cualquiera de las ya seleccionadas como especiales.
        /// </summary>
        private static bool IsAdjacentToAny(BoardCellDto candidate, IList<BoardCellDto> selected)
        {
            foreach (BoardCellDto cell in selected)
            {
                if (AreAdjacent(cell, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Dos celdas son adyacentes si están pegadas horizontal, vertical
        /// o diagonalmente (diferencia de fila y columna <= 1).
        /// </summary>
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
