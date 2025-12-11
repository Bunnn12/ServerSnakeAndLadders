using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Gameboard;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class GameBoardBuilderTests
    {
        private const int VALID_GAME_ID = 1;

        private const string DIFFICULTY_MEDIUM = "medium";

        private const int EIGHT_BY_EIGHT_ROWS = 8;
        private const int EIGHT_BY_EIGHT_COLUMNS = 8;

        private const int TEN_BY_TEN_ROWS = 10;
        private const int TEN_BY_TEN_COLUMNS = 10;

        private const int TWELVE_BY_TWELVE_ROWS = 12;
        private const int TWELVE_BY_TWELVE_COLUMNS = 12;

        private readonly GameBoardBuilder _builder;

        public GameBoardBuilderTests()
        {
            var layoutBuilder = new BoardLayoutBuilder();
            var specialCellsAssigner = new SpecialCellsAssigner();
            var snakesAndLaddersPlacer = new SnakesAndLaddersPlacer();

            _builder = new GameBoardBuilder(
                layoutBuilder,
                specialCellsAssigner,
                snakesAndLaddersPlacer);
        }


        [Fact]
        public void TestConstructorThrowsWhenLayoutBuilderIsNull()
        {
            var specialCellsAssigner = new SpecialCellsAssigner();
            var snakesAndLaddersPlacer = new SnakesAndLaddersPlacer();

            var ex = Assert.Throws<ArgumentNullException>(
                () => new GameBoardBuilder(
                    null,
                    specialCellsAssigner,
                    snakesAndLaddersPlacer));

            bool isOk = ex.ParamName == "layoutBuilder";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenSpecialCellsAssignerIsNull()
        {
            var layoutBuilder = new BoardLayoutBuilder();
            var snakesAndLaddersPlacer = new SnakesAndLaddersPlacer();

            var ex = Assert.Throws<ArgumentNullException>(
                () => new GameBoardBuilder(
                    layoutBuilder,
                    null,
                    snakesAndLaddersPlacer));

            bool isOk = ex.ParamName == "specialCellsAssigner";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenSnakesAndLaddersPlacerIsNull()
        {
            var layoutBuilder = new BoardLayoutBuilder();
            var specialCellsAssigner = new SpecialCellsAssigner();

            var ex = Assert.Throws<ArgumentNullException>(
                () => new GameBoardBuilder(
                    layoutBuilder,
                    specialCellsAssigner,
                    null));

            bool isOk = ex.ParamName == "snakesAndLaddersPlacer";

            Assert.True(isOk);
        }


        [Fact]
        public void TestBuildBoardThrowsWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _builder.BuildBoard(null));

            bool isOk = ex.ParamName == "request";

            Assert.True(isOk);
        }

        [Fact]
        public void TestBuildBoardThrowsWhenGameIdIsInvalid()
        {
            var request = CreateValidRequest(BoardSizeOption.EightByEight);
            request.GameId = 0;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => _builder.BuildBoard(request));

            bool isOk =
                ex.ParamName == "request" &&
                ex.Message.Contains("GameId");

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
        public void TestBuildBoardCreatesBoardWithExpectedDimensions(
            BoardSizeOption boardSize,
            int expectedRows,
            int expectedColumns)
        {
            var request = CreateValidRequest(boardSize);

            BoardDefinitionDto board = _builder.BuildBoard(request);

            bool isOk =
                board != null &&
                board.BoardSize == boardSize &&
                board.Rows == expectedRows &&
                board.Columns == expectedColumns &&
                board.Cells != null &&
                board.Cells.Count == expectedRows * expectedColumns;

            Assert.True(isOk);
        }


        [Fact]
        public void TestBuildBoardMarksStartAndFinalCellsCorrectly()
        {
            var request = CreateValidRequest(BoardSizeOption.TenByTen);

            BoardDefinitionDto board = _builder.BuildBoard(request);

            BoardCellDto startCell = board.Cells
                .FirstOrDefault(c => c.Index == 1);

            int lastIndex = board.Cells.Count;
            BoardCellDto finalCell = board.Cells
                .FirstOrDefault(c => c.Index == lastIndex);

            int startCount = board.Cells.Count(c => c.IsStart);
            int finalCount = board.Cells.Count(c => c.IsFinal);
            bool hasCellWithBothFlags = board.Cells.Any(
                c => c.IsStart && c.IsFinal);

            bool isOk =
                board != null &&
                startCell != null &&
                finalCell != null &&
                startCell.IsStart &&
                !startCell.IsFinal &&
                finalCell.IsFinal &&
                !finalCell.IsStart &&
                startCount == 1 &&
                finalCount == 1 &&
                !hasCellWithBothFlags;

            Assert.True(isOk);
        }

        [Fact]
        public void TestBuildBoardCreatesLinksWithinPlayableRange()
        {
            var request = CreateValidRequest(BoardSizeOption.TenByTen);

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int totalCells = board.Cells.Count;
            bool hasInvalidLink = false;

            if (board.Links != null && board.Links.Count > 0)
            {
                foreach (BoardLinkDto link in board.Links)
                {
                    bool startInRange =
                        link.StartIndex > 1 &&
                        link.StartIndex < totalCells;

                    bool endInRange =
                        link.EndIndex > 1 &&
                        link.EndIndex < totalCells;

                    if (!startInRange || !endInRange)
                    {
                        hasInvalidLink = true;
                        break;
                    }
                }
            }

            bool isOk =
                board != null &&
                board.Links != null &&
                !hasInvalidLink;

            Assert.True(isOk);
        }



        private static CreateBoardRequestDto CreateValidRequest(
            BoardSizeOption boardSize)
        {
            return new CreateBoardRequestDto
            {
                GameId = VALID_GAME_ID,
                BoardSize = boardSize,
                Difficulty = DIFFICULTY_MEDIUM,
                EnableDiceCells = true,
                EnableItemCells = true,
                EnableMessageCells = true
            };
        }
    }
}
