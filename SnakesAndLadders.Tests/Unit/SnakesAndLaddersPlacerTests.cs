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
    public sealed class SnakesAndLaddersPlacerTests
    {
        private const string DIFFICULTY_MEDIUM = "medium";

        private const int MIN_CELL_INDEX = 1;

        private readonly SnakesAndLaddersPlacer _placer;
        private readonly BoardLayoutBuilder _layoutBuilder;
        private readonly SpecialCellsAssigner _specialCellsAssigner;

        public SnakesAndLaddersPlacerTests()
        {
            _placer = new SnakesAndLaddersPlacer();
            _layoutBuilder = new BoardLayoutBuilder();
            _specialCellsAssigner = new SpecialCellsAssigner();
        }


        [Fact]
        public void TestAddSnakesAndLaddersThrowsWhenCellsIsNull()
        {
            var board = new BoardDefinitionDto
            {
                BoardSize = BoardSizeOption.TenByTen
            };

            var ex = Assert.Throws<ArgumentNullException>(
                () => _placer.AddSnakesAndLadders(
                    null,
                    board,
                    DIFFICULTY_MEDIUM));

            bool isOk = ex.ParamName == "cells";

            Assert.True(isOk);
        }

        [Fact]
        public void TestAddSnakesAndLaddersThrowsWhenBoardIsNull()
        {
            var cells = new List<BoardCellDto>();

            var ex = Assert.Throws<ArgumentNullException>(
                () => _placer.AddSnakesAndLadders(
                    cells,
                    null,
                    DIFFICULTY_MEDIUM));

            bool isOk = ex.ParamName == "board";

            Assert.True(isOk);
        }

        [Fact]
        public void TestAddSnakesAndLaddersCreatesEmptyLinksWhenCellsIsEmpty()
        {
            var cells = new List<BoardCellDto>();

            var board = new BoardDefinitionDto
            {
                BoardSize = BoardSizeOption.TenByTen
            };

            _placer.AddSnakesAndLadders(
                cells,
                board,
                DIFFICULTY_MEDIUM);

            bool isOk =
                board.Links != null &&
                board.Links.Count == 0;

            Assert.True(isOk);
        }


        [Fact]
        public void TestAddSnakesAndLaddersCreatesLinksWithinPlayableRangeAndWithoutOverlap()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.TenByTen);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var board = new BoardDefinitionDto
            {
                BoardSize = BoardSizeOption.TenByTen,
                Rows = layout.Rows,
                Columns = layout.Columns,
                Cells = cells
            };

            var enabledSpecialTypes = new[]
            {
                SpecialCellType.Dice,
                SpecialCellType.Item,
                SpecialCellType.Message
            };

            _specialCellsAssigner.AssignSpecialCells(
                cells,
                BoardSizeOption.TenByTen,
                enabledSpecialTypes);

            _placer.AddSnakesAndLadders(
                cells,
                board,
                DIFFICULTY_MEDIUM);

            int totalCells = cells.Count;

            var allIndices = new List<int>();

            if (board.Links != null)
            {
                foreach (BoardLinkDto link in board.Links)
                {
                    allIndices.Add(link.StartIndex);
                    allIndices.Add(link.EndIndex);
                }
            }

            bool hasLinks =
                board.Links != null &&
                board.Links.Count > 0;

            bool indicesInRange = allIndices.All(
                index =>
                    index > MIN_CELL_INDEX &&
                    index < totalCells);

            bool hasNoOverlap =
                allIndices.Count ==
                allIndices.Distinct().Count();

            bool isOk =
                board != null &&
                hasLinks &&
                indicesInRange &&
                hasNoOverlap;

            Assert.True(isOk);
        }



        [Fact]
        public void TestAddSnakesAndLaddersCreatesValidGeometryForSnakesAndLadders()
        {
            var layout = _layoutBuilder.BuildLayout(BoardSizeOption.TwelveByTwelve);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            var board = new BoardDefinitionDto
            {
                BoardSize = BoardSizeOption.TwelveByTwelve,
                Rows = layout.Rows,
                Columns = layout.Columns,
                Cells = cells
            };

            var enabledSpecialTypes = new[]
            {
                SpecialCellType.Dice,
                SpecialCellType.Item,
                SpecialCellType.Message
            };

            _specialCellsAssigner.AssignSpecialCells(
                cells,
                BoardSizeOption.TwelveByTwelve,
                enabledSpecialTypes);

            _placer.AddSnakesAndLadders(
                cells,
                board,
                DIFFICULTY_MEDIUM);

            int totalCells = cells.Count;
            var cellByIndex = cells.ToDictionary(c => c.Index);

            var ladders = board.Links
                .Where(l => l.IsLadder)
                .ToList();

            var snakes = board.Links
                .Where(l => !l.IsLadder)
                .ToList();

            bool laddersOk = ladders.All(link =>
            {
                BoardCellDto startCell = cellByIndex[link.StartIndex];
                BoardCellDto endCell = cellByIndex[link.EndIndex];

                bool indexOrder =
                    link.StartIndex < link.EndIndex;

                bool rowOrder =
                    startCell.Row > endCell.Row;

                bool notStartOrFinal =
                    link.StartIndex != MIN_CELL_INDEX &&
                    link.EndIndex != MIN_CELL_INDEX &&
                    link.StartIndex != totalCells &&
                    link.EndIndex != totalCells;

                return indexOrder && rowOrder && notStartOrFinal;
            });

            bool snakesOk = snakes.All(link =>
            {
                BoardCellDto startCell = cellByIndex[link.StartIndex];
                BoardCellDto endCell = cellByIndex[link.EndIndex];

                bool indexOrder =
                    link.StartIndex > link.EndIndex;

                bool rowOrder =
                    startCell.Row < endCell.Row;

                bool notStartOrFinal =
                    link.StartIndex != MIN_CELL_INDEX &&
                    link.EndIndex != MIN_CELL_INDEX &&
                    link.StartIndex != totalCells &&
                    link.EndIndex != totalCells;

                return indexOrder && rowOrder && notStartOrFinal;
            });

            bool isOk =
                board != null &&
                board.Links != null &&
                board.Links.Count > 0 &&
                laddersOk &&
                snakesOk;

            Assert.True(isOk);
        }

    }
}
