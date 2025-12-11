using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic.Gameboard;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class SpecialCellsAssignerTests
    {
        private const int VALID_GAME_ID = 1;

        private readonly SpecialCellsAssigner _assigner;
        private readonly BoardLayoutBuilder _layoutBuilder;

        public SpecialCellsAssignerTests()
        {
            _assigner = new SpecialCellsAssigner();
            _layoutBuilder = new BoardLayoutBuilder();
        }


        [Fact]
        public void TestGetEnabledSpecialTypesThrowsWhenRequestIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => _assigner.GetEnabledSpecialTypes(null));

            bool isOk = ex.ParamName == "request";

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetEnabledSpecialTypesReturnsEmptyWhenAllFlagsDisabled()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = VALID_GAME_ID,
                BoardSize = BoardSizeOption.EightByEight,
                EnableDiceCells = false,
                EnableItemCells = false,
                EnableMessageCells = false
            };

            var enabled = _assigner.GetEnabledSpecialTypes(request);

            bool isOk =
                enabled != null &&
                enabled.Count == 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetEnabledSpecialTypesReturnsOnlyEnabledTypes()
        {
            var request = new CreateBoardRequestDto
            {
                GameId = VALID_GAME_ID,
                BoardSize = BoardSizeOption.TenByTen,
                EnableDiceCells = true,
                EnableItemCells = false,
                EnableMessageCells = true
            };

            var enabled = _assigner.GetEnabledSpecialTypes(request);

            bool hasDice =
                enabled.Contains(SpecialCellType.Dice);

            bool hasMessage =
                enabled.Contains(SpecialCellType.Message);

            bool hasItem =
                enabled.Contains(SpecialCellType.Item);

            bool isOk =
                enabled.Count == 2 &&
                hasDice &&
                hasMessage &&
                !hasItem;

            Assert.True(isOk);
        }

        [Fact]
        public void TestAssignSpecialCellsThrowsWhenCellsIsNull()
        {
            var enabledTypes = new List<SpecialCellType>
            {
                SpecialCellType.Dice
            };

            var ex = Assert.Throws<ArgumentNullException>(
                () => _assigner.AssignSpecialCells(
                    null,
                    BoardSizeOption.EightByEight,
                    enabledTypes));

            bool isOk = ex.ParamName == "cells";

            Assert.True(isOk);
        }

        [Fact]
        public void TestAssignSpecialCellsDoesNothingWhenEnabledTypesIsNull()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.EightByEight);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            _assigner.AssignSpecialCells(
                cells,
                BoardSizeOption.EightByEight,
                null);

            int specialCount = cells.Count(
                c => c.SpecialType != SpecialCellType.None);

            bool isOk =
                specialCount == 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestAssignSpecialCellsDoesNothingWhenEnabledTypesIsEmpty()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.EightByEight);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var enabled = Array.Empty<SpecialCellType>();

            _assigner.AssignSpecialCells(
                cells,
                BoardSizeOption.EightByEight,
                enabled);

            int specialCount = cells.Count(
                c => c.SpecialType != SpecialCellType.None);

            bool isOk =
                specialCount == 0;

            Assert.True(isOk);
        }


        [Fact]
        public void TestAssignSpecialCellsForEightByEightWithOneTypeRespectsTotalSpecialCells()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.EightByEight);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var enabled = new[]
            {
                SpecialCellType.Dice
            };

            int expectedTotal =
                GameBoardBuilderConstants.SPECIAL_CELLS_8_ONE_TYPE;

            _assigner.AssignSpecialCells(
                cells,
                BoardSizeOption.EightByEight,
                enabled);

            int diceCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Dice);

            int otherSpecialCount = cells.Count(
                c => c.SpecialType != SpecialCellType.None &&
                     c.SpecialType != SpecialCellType.Dice);

            bool hasSpecialOnStart =
                cells.First(c => c.Index == GameBoardBuilderConstants.MIN_CELL_INDEX)
                    .SpecialType != SpecialCellType.None;

            bool hasSpecialOnFinal =
                cells.First(c => c.Index == layout.CellCount)
                    .SpecialType != SpecialCellType.None;

            bool isOk =
                diceCount == expectedTotal &&
                otherSpecialCount == 0 &&
                !hasSpecialOnStart &&
                !hasSpecialOnFinal;

            Assert.True(isOk);
        }

        [Fact]
        public void TestAssignSpecialCellsForTenByTenWithTwoTypesRespectsTotalSpecialCells()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.TenByTen);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var enabled = new[]
            {
                SpecialCellType.Dice,
                SpecialCellType.Item
            };

            int expectedTotal =
                GameBoardBuilderConstants.SPECIAL_CELLS_10_TWO_TYPES;

            _assigner.AssignSpecialCells(
                cells,
                BoardSizeOption.TenByTen,
                enabled);

            int diceCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Dice);

            int itemCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Item);

            int messageCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Message);

            int totalSpecial = diceCount + itemCount + messageCount;

            bool isOk =
                totalSpecial == expectedTotal &&
                messageCount == 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestAssignSpecialCellsForTwelveByTwelveWithThreeTypesRespectsTotalSpecialCells()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.TwelveByTwelve);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var enabled = new[]
            {
                SpecialCellType.Dice,
                SpecialCellType.Item,
                SpecialCellType.Message
            };

            int expectedTotal =
                GameBoardBuilderConstants.SPECIAL_CELLS_12_THREE_TYPES;

            _assigner.AssignSpecialCells(
                cells,
                BoardSizeOption.TwelveByTwelve,
                enabled);

            int diceCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Dice);

            int itemCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Item);

            int messageCount = cells.Count(
                c => c.SpecialType == SpecialCellType.Message);

            int totalSpecial = diceCount + itemCount + messageCount;

            bool isOk =
                totalSpecial == expectedTotal &&
                diceCount > 0 &&
                itemCount > 0 &&
                messageCount > 0;

            Assert.True(isOk);
        }


        [Fact]
        public void TestAssignSpecialCellsDoesNothingWhenBoardSizeUnsupported()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.TenByTen);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var enabled = new[]
            {
                SpecialCellType.Dice
            };

            var unsupportedSize = (BoardSizeOption)999;

            _assigner.AssignSpecialCells(
                cells,
                unsupportedSize,
                enabled);

            int specialCount = cells.Count(
                c => c.SpecialType != SpecialCellType.None);

            bool isOk =
                specialCount == 0;

            Assert.True(isOk);
        }

    }
}
