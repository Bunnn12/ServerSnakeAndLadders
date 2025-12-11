using log4net;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;
using SnakesAndLadders.Services.Logic.Gameboard;
using System;
using System.Collections.Generic;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class GameBoardBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardBuilder));

        private readonly BoardLayoutBuilder _layoutBuilder;
        private readonly SpecialCellsAssigner _specialCellsAssigner;
        private readonly SnakesAndLaddersPlacer _snakesAndLaddersPlacer;

        public GameBoardBuilder(
            BoardLayoutBuilder layoutBuilder,
            SpecialCellsAssigner specialCellsAssigner,
            SnakesAndLaddersPlacer snakesAndLaddersPlacer)
        {
            _layoutBuilder = layoutBuilder
                             ?? throw new ArgumentNullException(nameof(layoutBuilder));

            _specialCellsAssigner = specialCellsAssigner
                                    ?? throw new ArgumentNullException(nameof(specialCellsAssigner));

            _snakesAndLaddersPlacer = snakesAndLaddersPlacer
                                      ?? throw new ArgumentNullException(nameof(snakesAndLaddersPlacer));
        }

        public BoardDefinitionDto BuildBoard(CreateBoardRequestDto request)
        {
            if (request == null)
            {
                Logger.Error(GameBoardBuilderConstants.LOG_ERROR_NULL_REQUEST);
                throw new ArgumentNullException(nameof(request));
            }

            if (request.GameId <= 0)
            {
                Logger.ErrorFormat(
                    GameBoardBuilderConstants.LOG_ERROR_INVALID_GAME_ID,
                    request.GameId);

                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "GameId must be greater than zero.");
            }

            BoardLayoutDefinition layout = _layoutBuilder.BuildLayout(request.BoardSize);
            IList<BoardCellDto> cells = _layoutBuilder.CreateCells(layout);

            IReadOnlyCollection<SpecialCellType> enabledTypes =
                _specialCellsAssigner.GetEnabledSpecialTypes(request);

            _specialCellsAssigner.AssignSpecialCells(
                cells,
                request.BoardSize,
                enabledTypes);

            var board = new BoardDefinitionDto
            {
                BoardSize = request.BoardSize,
                Rows = layout.Rows,
                Columns = layout.Columns,
                Cells = cells
            };

            _snakesAndLaddersPlacer.AddSnakesAndLadders(
                cells,
                board,
                request.Difficulty);

            Logger.InfoFormat(
                GameBoardBuilderConstants.LOG_INFO_BOARD_CREATED,
                request.BoardSize,
                layout.Rows,
                layout.Columns);

            return board;
        }
    }
}
