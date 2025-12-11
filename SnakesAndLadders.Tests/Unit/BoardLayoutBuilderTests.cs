using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Logic.Gameboard;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class BoardLayoutBuilderTests
    {
        private const int EIGHT_BY_EIGHT_ROWS = 8;
        private const int EIGHT_BY_EIGHT_COLUMNS = 8;

        private const int TEN_BY_TEN_ROWS = 10;
        private const int TEN_BY_TEN_COLUMNS = 10;

        private const int TWELVE_BY_TWELVE_ROWS = 12;
        private const int TWELVE_BY_TWELVE_COLUMNS = 12;

        private const int MIN_CELL_INDEX = 1;

        private readonly BoardLayoutBuilder _builder;

        public BoardLayoutBuilderTests()
        {
            _builder = new BoardLayoutBuilder();
        }


        [Fact]
        public void TestBoardLayoutDefinitionThrowsWhenRowsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardLayoutDefinition(0, 5));

            bool isOk = ex.ParamName == "rows";

            Assert.True(isOk);
        }

        [Fact]
        public void TestBoardLayoutDefinitionThrowsWhenColumnsInvalid()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardLayoutDefinition(5, 0));

            bool isOk = ex.ParamName == "columns";

            Assert.True(isOk);
        }

        [Fact]
        public void TestBoardLayoutDefinitionSetsPropertiesCorrectly()
        {
            const int rows = 4;
            const int columns = 6;

            var layout = new BoardLayoutDefinition(rows, columns);

            bool isOk =
                layout.Rows == rows &&
                layout.Columns == columns &&
                layout.CellCount == rows * columns;

            Assert.True(isOk);
        }



        [Theory]
        [InlineData(BoardSizeOption.EightByEight,
            EIGHT_BY_EIGHT_ROWS,
            EIGHT_BY_EIGHT_COLUMNS)]
        [InlineData(BoardSizeOption.TenByTen,
            TEN_BY_TEN_ROWS,
            TEN_BY_TEN_COLUMNS)]
        [InlineData(BoardSizeOption.TwelveByTwelve,
            TWELVE_BY_TWELVE_ROWS,
            TWELVE_BY_TWELVE_COLUMNS)]
        public void TestBuildLayoutReturnsExpectedDimensions(
            BoardSizeOption boardSize,
            int expectedRows,
            int expectedColumns)
        {
            BoardLayoutDefinition layout = _builder.BuildLayout(boardSize);

            bool isOk =
                layout != null &&
                layout.Rows == expectedRows &&
                layout.Columns == expectedColumns &&
                layout.CellCount == expectedRows * expectedColumns;

            Assert.True(isOk);
        }

        [Fact]
        public void TestBuildLayoutThrowsWhenBoardSizeUnsupported()
        {
            var invalidSize = (BoardSizeOption)999;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _builder.BuildLayout(invalidSize));

            bool isOk = ex.ParamName == "boardSize";

            Assert.True(isOk);
        }

 

        [Fact]
        public void TestCreateCellsThrowsWhenLayoutIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _builder.CreateCells(null));

            bool isOk = ex.ParamName == "layout";

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateCellsCreatesExpectedNumberOfCells()
        {
            var layout = new BoardLayoutDefinition(
                EIGHT_BY_EIGHT_ROWS,
                EIGHT_BY_EIGHT_COLUMNS);

            IList<BoardCellDto> cells = _builder.CreateCells(layout);

            bool isOk =
                cells != null &&
                cells.Count == layout.CellCount;

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateCellsMarksStartAndFinalCorrectly()
        {
            var layout = new BoardLayoutDefinition(
                TEN_BY_TEN_ROWS,
                TEN_BY_TEN_COLUMNS);

            IList<BoardCellDto> cells = _builder.CreateCells(layout);

            BoardCellDto startCell = cells
                .FirstOrDefault(c => c.Index == MIN_CELL_INDEX);

            BoardCellDto finalCell = cells
                .FirstOrDefault(c => c.Index == layout.CellCount);

            int startCount = cells.Count(c => c.IsStart);
            int finalCount = cells.Count(c => c.IsFinal);
            bool hasBothFlags = cells.Any(
                c => c.IsStart && c.IsFinal);

            bool isOk =
                startCell != null &&
                finalCell != null &&
                startCell.IsStart &&
                !startCell.IsFinal &&
                finalCell.IsFinal &&
                !finalCell.IsStart &&
                startCount == 1 &&
                finalCount == 1 &&
                !hasBothFlags;

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateCellsAssignsSequentialIndexesWithoutGaps()
        {
            var layout = new BoardLayoutDefinition(
                TWELVE_BY_TWELVE_ROWS,
                TWELVE_BY_TWELVE_COLUMNS);

            IList<BoardCellDto> cells = _builder.CreateCells(layout);

            var ordered = cells
                .Select(c => c.Index)
                .OrderBy(i => i)
                .ToList();

            bool hasExpectedCount =
                ordered.Count == layout.CellCount;

            bool startsAtMin =
                ordered.First() == MIN_CELL_INDEX;

            bool endsAtMax =
                ordered.Last() == layout.CellCount;

            bool hasNoGaps =
                ordered.Zip(
                        ordered.Skip(1),
                        (prev, next) => next - prev)
                    .All(delta => delta == 1);

            bool isOk =
                hasExpectedCount &&
                startsAtMin &&
                endsAtMax &&
                hasNoGaps;

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateCellsInitializesCellsWithNoSpecialType()
        {
            var layout = new BoardLayoutDefinition(
                EIGHT_BY_EIGHT_ROWS,
                EIGHT_BY_EIGHT_COLUMNS);

            IList<BoardCellDto> cells = _builder.CreateCells(layout);

            bool isOk =
                cells.All(
                    c => c.SpecialType == SpecialCellType.None);

            Assert.True(isOk);
        }

        [Fact]
        public void TestCreateCellsAssignsBothDarkAndLightCells()
        {
            var layout = new BoardLayoutDefinition(
                TEN_BY_TEN_ROWS,
                TEN_BY_TEN_COLUMNS);

            IList<BoardCellDto> cells = _builder.CreateCells(layout);

            bool hasDark = cells.Any(c => c.IsDark);
            bool hasLight = cells.Any(c => !c.IsDark);

            bool isOk =
                cells.Count == layout.CellCount &&
                hasDark &&
                hasLight;

            Assert.True(isOk);
        }

    }
}
