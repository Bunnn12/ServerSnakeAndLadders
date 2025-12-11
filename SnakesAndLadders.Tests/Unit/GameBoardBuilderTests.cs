using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Logic;
using System;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    /*
    public sealed class GameBoardBuilderTests
    {
        private const int BOARD_8_ROWS = 8;
        private const int BOARD_8_COLUMNS = 8;

        private const int BOARD_10_ROWS = 10;
        private const int BOARD_10_COLUMNS = 10;

        private const int BOARD_12_ROWS = 12;
        private const int BOARD_12_COLUMNS = 12;

        private const int SPECIAL_8_ONE_TYPE = 3;
        private const int SPECIAL_8_TWO_TYPES = 4;
        private const int SPECIAL_8_THREE_TYPES = 6;

        private const int SPECIAL_10_ONE_TYPE = 4;
        private const int SPECIAL_10_TWO_TYPES = 8;
        private const int SPECIAL_10_THREE_TYPES = 12;

        private const int SPECIAL_12_ONE_TYPE = 5;
        private const int SPECIAL_12_TWO_TYPES = 12;
        private const int SPECIAL_12_THREE_TYPES = 15;

        private const int LADDERS_8 = 4;
        private const int LADDERS_10 = 5;
        private const int LADDERS_12 = 6;

        private const int SNAKES_8_EASY = 4;
        private const int SNAKES_8_MEDIUM = 5;
        private const int SNAKES_8_HARD = 6;

        private const int SNAKES_10_EASY = 5;
        private const int SNAKES_10_MEDIUM = 6;
        private const int SNAKES_10_HARD = 7;

        private const int SNAKES_12_EASY = 6;
        private const int SNAKES_12_MEDIUM = 7;
        private const int SNAKES_12_HARD = 8;

        private readonly GameBoardBuilder _builder;

        public GameBoardBuilderTests()
        {
            _builder = new GameBoardBuilder();
        }

        #region Validación de request

        [Fact]
        public void TestBuildBoardThrowsWhenRequestIsNull()
        {
            // Act + Assert (único assert)
            Assert.Throws<ArgumentNullException>(() => _builder.BuildBoard(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestBuildBoardThrowsWhenGameIdInvalid(int gameId)
        {
            var request = new CreateBoardRequestDto
            {
                GameId = gameId,
                BoardSize = BoardSizeOption.EightByEight,
                Difficulty = "medium"
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildBoard(request));
        }

        #endregion

        #region Layout básico por tamaño

        [Theory]
        [InlineData(BoardSizeOption.EightByEight, BOARD_8_ROWS, BOARD_8_COLUMNS)]
        [InlineData(BoardSizeOption.TenByTen, BOARD_10_ROWS, BOARD_10_COLUMNS)]
        [InlineData(BoardSizeOption.TwelveByTwelve, BOARD_12_ROWS, BOARD_12_COLUMNS)]
        public void TestBuildBoardCreatesCorrectRowsColumnsAndCellCount(
            BoardSizeOption size,
            int expectedRows,
            int expectedColumns)
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = size,
                Difficulty = "medium"
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            bool ok =
                board.BoardSize == size &&
                board.Rows == expectedRows &&
                board.Columns == expectedColumns &&
                board.Cells != null &&
                board.Cells.Count == expectedRows * expectedColumns;

            Assert.True(ok);
        }

        [Fact]
        public void TestBuildBoardMarksStartAndFinalCellsCorrectly()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.EightByEight,
                Difficulty = "medium"
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            bool ok =
                board.Cells.Count > 0 &&
                board.Cells.Count(c => c.IsStart) == 1 &&
                board.Cells.Count(c => c.IsFinal) == 1 &&
                board.Cells.Single(c => c.IsStart).Index == 1 &&
                board.Cells.Single(c => c.IsFinal).Index == board.Cells.Count;

            Assert.True(ok);
        }

        #endregion

        #region Casillas especiales (cantidad)

        [Theory]
        [InlineData(BoardSizeOption.EightByEight, true, false, false, SPECIAL_8_ONE_TYPE)]
        [InlineData(BoardSizeOption.EightByEight, true, true, false, SPECIAL_8_TWO_TYPES)]
        [InlineData(BoardSizeOption.EightByEight, true, true, true, SPECIAL_8_THREE_TYPES)]
        [InlineData(BoardSizeOption.TenByTen, true, false, false, SPECIAL_10_ONE_TYPE)]
        [InlineData(BoardSizeOption.TenByTen, true, true, false, SPECIAL_10_TWO_TYPES)]
        [InlineData(BoardSizeOption.TenByTen, true, true, true, SPECIAL_10_THREE_TYPES)]
        [InlineData(BoardSizeOption.TwelveByTwelve, true, false, false, SPECIAL_12_ONE_TYPE)]
        [InlineData(BoardSizeOption.TwelveByTwelve, true, true, false, SPECIAL_12_TWO_TYPES)]
        [InlineData(BoardSizeOption.TwelveByTwelve, true, true, true, SPECIAL_12_THREE_TYPES)]
        public void TestBuildBoardCreatesExpectedNumberOfSpecialCells(
            BoardSizeOption size,
            bool enableDice,
            bool enableItem,
            bool enableMessage,
            int expectedSpecialTotal)
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = size,
                Difficulty = "medium",
                EnableDiceCells = enableDice,
                EnableItemCells = enableItem,
                EnableMessageCells = enableMessage
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int specialCount = board.Cells.Count(c => c.SpecialType != SpecialCellType.None);

            Assert.True(specialCount == expectedSpecialTotal);
        }

        [Fact]
        public void TestBuildBoardCreatesNoSpecialCellsWhenAllDisabled()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.EightByEight,
                Difficulty = "medium",
                EnableDiceCells = false,
                EnableItemCells = false,
                EnableMessageCells = false
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int specialCount = board.Cells.Count(c => c.SpecialType != SpecialCellType.None);

            Assert.True(specialCount == 0);
        }

        #endregion

        #region Snakes y ladders – cantidades

        [Theory]
        // 8x8
        [InlineData(BoardSizeOption.EightByEight, "easy", LADDERS_8, SNAKES_8_EASY)]
        [InlineData(BoardSizeOption.EightByEight, "medium", LADDERS_8, SNAKES_8_MEDIUM)]
        [InlineData(BoardSizeOption.EightByEight, "hard", LADDERS_8, SNAKES_8_HARD)]
        // 10x10
        [InlineData(BoardSizeOption.TenByTen, "easy", LADDERS_10, SNAKES_10_EASY)]
        [InlineData(BoardSizeOption.TenByTen, "medium", LADDERS_10, SNAKES_10_MEDIUM)]
        [InlineData(BoardSizeOption.TenByTen, "hard", LADDERS_10, SNAKES_10_HARD)]
        // 12x12
        [InlineData(BoardSizeOption.TwelveByTwelve, "easy", LADDERS_12, SNAKES_12_EASY)]
        [InlineData(BoardSizeOption.TwelveByTwelve, "medium", LADDERS_12, SNAKES_12_MEDIUM)]
        [InlineData(BoardSizeOption.TwelveByTwelve, "hard", LADDERS_12, SNAKES_12_HARD)]
        public void TestBuildBoardCreatesExpectedNumberOfSnakesAndLadders(
            BoardSizeOption size,
            string difficulty,
            int expectedLadders,
            int expectedSnakes)
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = size,
                Difficulty = difficulty,
                EnableDiceCells = true,
                EnableItemCells = true,
                EnableMessageCells = true
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int ladders = board.Links.Count(l => l.IsLadder);
            int snakes = board.Links.Count(l => !l.IsLadder);

            bool ok = ladders == expectedLadders && snakes == expectedSnakes;

            Assert.True(ok);
        }

        #endregion

        #region Difficulty normalization

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("MEDIUM")]
        [InlineData(" Medium ")]
        public void TestDifficultyIsNormalizedToMediumWhenNullEmptyOrMediumLike(string difficulty)
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.EightByEight,
                Difficulty = difficulty,
                EnableDiceCells = false,
                EnableItemCells = false,
                EnableMessageCells = false
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int snakes = board.Links.Count(l => !l.IsLadder);

            Assert.True(snakes == SNAKES_8_MEDIUM);
        }

        #endregion

        #region Ladders rules

        [Fact]
        public void TestLaddersRespectPlacementRules()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.TenByTen,
                Difficulty = "medium",
                EnableDiceCells = true,
                EnableItemCells = true,
                EnableMessageCells = true
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int totalCells = board.Cells.Count;
            var cellByIndex = board.Cells.ToDictionary(c => c.Index);
            var ladders = board.Links.Where(l => l.IsLadder).ToList();

            bool ok = ladders.All(link =>
            {
                BoardCellDto start = cellByIndex[link.StartIndex];
                BoardCellDto end = cellByIndex[link.EndIndex];

                return link.StartIndex != link.EndIndex
                       && start.Row > end.Row
                       && start.SpecialType == SpecialCellType.None
                       && end.SpecialType == SpecialCellType.None
                       && link.StartIndex != 1
                       && link.EndIndex != totalCells;
            });

            Assert.True(ok);
        }

        #endregion

        #region Snakes rules

        [Fact]
        public void TestSnakesRespectPlacementRules()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.TwelveByTwelve,
                Difficulty = "hard",
                EnableDiceCells = true,
                EnableItemCells = true,
                EnableMessageCells = true
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            int totalCells = board.Cells.Count;
            var cellByIndex = board.Cells.ToDictionary(c => c.Index);
            var snakes = board.Links.Where(l => !l.IsLadder).ToList();

            bool ok = snakes.All(link =>
            {
                BoardCellDto start = cellByIndex[link.StartIndex];
                BoardCellDto end = cellByIndex[link.EndIndex];

                return link.StartIndex != link.EndIndex
                       && start.Row < end.Row
                       && start.SpecialType == SpecialCellType.None
                       && end.SpecialType == SpecialCellType.None
                       && link.StartIndex != totalCells
                       && link.EndIndex != 1;
            });

            Assert.True(ok);
        }

        #endregion

        #region Links uniqueness

        [Fact]
        public void TestNoTwoLinksShareStartOrEndIndex()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = BoardSizeOption.TenByTen,
                Difficulty = "medium",
                EnableDiceCells = true,
                EnableItemCells = true,
                EnableMessageCells = true
            };

            BoardDefinitionDto board = _builder.BuildBoard(request);

            var used = new System.Collections.Generic.HashSet<int>();
            bool allDistinct = board.Links.All(link =>
                used.Add(link.StartIndex) && used.Add(link.EndIndex));

            Assert.True(allDistinct);
        }

        #endregion

        #region Unsupported board size

        [Fact]
        public void TestBuildBoardThrowsWhenBoardSizeUnsupported()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = 1,
                BoardSize = (BoardSizeOption)999,
                Difficulty = "medium"
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => _builder.BuildBoard(request));
        }

        #endregion

    }
    */
}
