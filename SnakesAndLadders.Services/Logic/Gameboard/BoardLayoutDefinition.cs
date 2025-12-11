using System;
using System.Collections.Generic;
using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic.Gameboard
{
    public sealed class BoardLayoutDefinition
    {
        public BoardLayoutDefinition(int rows, int columns)
        {
            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            Rows = rows;
            Columns = columns;
            CellCount = rows * columns;
        }

        public int Rows { get; }

        public int Columns { get; }

        public int CellCount { get; }
    }

    public sealed class BoardLayoutBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BoardLayoutBuilder));

        public BoardLayoutDefinition BuildLayout(BoardSizeOption boardSize)
        {
            switch (boardSize)
            {
                case BoardSizeOption.EightByEight:
                    return new BoardLayoutDefinition(
                        GameBoardBuilderConstants.BOARD_SIZE_8_ROWS,
                        GameBoardBuilderConstants.BOARD_SIZE_8_COLUMNS);

                case BoardSizeOption.TenByTen:
                    return new BoardLayoutDefinition(
                        GameBoardBuilderConstants.BOARD_SIZE_10_ROWS,
                        GameBoardBuilderConstants.BOARD_SIZE_10_COLUMNS);

                case BoardSizeOption.TwelveByTwelve:
                    return new BoardLayoutDefinition(
                        GameBoardBuilderConstants.BOARD_SIZE_12_ROWS,
                        GameBoardBuilderConstants.BOARD_SIZE_12_COLUMNS);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(boardSize),
                        "Unsupported board size.");
            }
        }

        public IList<BoardCellDto> CreateCells(BoardLayoutDefinition layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var cells = new List<BoardCellDto>(layout.CellCount);

            int currentIndex = GameBoardBuilderConstants.MIN_CELL_INDEX;

            for (int row = layout.Rows - 1; row >= 0; row--)
            {
                int distanceFromBottom = layout.Rows - 1 - row;
                bool isLeftToRight = (distanceFromBottom % GameBoardBuilderConstants.COLOR_PATTERN_MODULO) == 0;

                if (isLeftToRight)
                {
                    for (int column = 0; column < layout.Columns; column++)
                    {
                        int viewRow = distanceFromBottom;
                        int viewColumn = column;
                        bool isDark = ((viewRow + viewColumn) % GameBoardBuilderConstants.COLOR_PATTERN_MODULO) == 0;

                        var cell = new BoardCellDto
                        {
                            Index = currentIndex,
                            Row = row,
                            Column = column,
                            IsDark = isDark,
                            SpecialType = SpecialCellType.None,
                            IsStart = currentIndex == GameBoardBuilderConstants.MIN_CELL_INDEX,
                            IsFinal = currentIndex == layout.CellCount
                        };

                        cells.Add(cell);
                        currentIndex++;
                    }
                }
                else
                {
                    for (int column = layout.Columns - 1; column >= 0; column--)
                    {
                        int viewRow = distanceFromBottom;
                        int viewColumn = layout.Columns - 1 - column;
                        bool isDark = ((viewRow + viewColumn) % GameBoardBuilderConstants.COLOR_PATTERN_MODULO) == 0;

                        var cell = new BoardCellDto
                        {
                            Index = currentIndex,
                            Row = row,
                            Column = column,
                            IsDark = isDark,
                            SpecialType = SpecialCellType.None,
                            IsStart = currentIndex == GameBoardBuilderConstants.MIN_CELL_INDEX,
                            IsFinal = currentIndex == layout.CellCount
                        };

                        cells.Add(cell);
                        currentIndex++;
                    }
                }
            }

            Logger.InfoFormat(
                "Board cells created. Rows={0}, Columns={1}, TotalCells={2}",
                layout.Rows,
                layout.Columns,
                layout.CellCount);

            return cells;
        }
    }
}
