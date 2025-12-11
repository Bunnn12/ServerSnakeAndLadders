using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Logic;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class GameplayLogicTests
    {
        private const int USER1_ID = 1;
        private const int USER2_ID = 2;

        private const string ERROR_AT_LEAST_ONE_PLAYER_REQUIRED =
            "GameplayLogic requires at least one player.";

        private const string ERROR_GAME_ALREADY_FINISHED_EN =
            "The game has already finished.";
        private const string ERROR_NO_PLAYERS_EN =
            "There are no players in the game.";



        [Fact]
        public void TestConstructorThrowsWhenBoardIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new GameplayLogic(null, new[] { USER1_ID }));

            bool isOk = ex.ParamName == "board";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenPlayerUserIdsIsNull()
        {
            BoardDefinitionDto board = CreateSimpleBoard(10);

            var ex = Assert.Throws<ArgumentNullException>(
                () => new GameplayLogic(board, null));

            bool isOk = ex.ParamName == "playerUserIds";

            Assert.True(isOk);
        }

        [Fact]
        public void TestConstructorThrowsWhenThereAreNoValidPlayers()
        {
            BoardDefinitionDto board = CreateSimpleBoard(10);

            var ex = Assert.Throws<InvalidOperationException>(
                () => new GameplayLogic(board, new int[0]));

            bool isOk = ex.Message == ERROR_AT_LEAST_ONE_PLAYER_REQUIRED;

            Assert.True(isOk);
        }



        [Fact]
        public void TestGetCurrentStateReturnsInitialSnapshotWithAllPlayers()
        {
            BoardDefinitionDto board = CreateSimpleBoard(20);

            var logic = new GameplayLogic(
                board,
                new[] { USER1_ID, USER2_ID });

            GameStateSnapshot snapshot = logic.GetCurrentState();

            bool hasUser1 = snapshot.Tokens.Any(t => t.UserId == USER1_ID);
            bool hasUser2 = snapshot.Tokens.Any(t => t.UserId == USER2_ID);

            int startCell = snapshot.Tokens.First().CellIndex;

            bool allSameCell = snapshot
                .Tokens
                .All(t => t.CellIndex == startCell);

            bool isOk =
                snapshot != null &&
                !snapshot.IsFinished &&
                snapshot.CurrentTurnUserId == USER1_ID &&
                snapshot.Tokens.Count == 2 &&
                hasUser1 &&
                hasUser2 &&
                allSameCell;

            Assert.True(isOk);
        }


        [Fact]
        public void TestHandleTurnTimeoutAdvancesTurnWhenGameNotFinished()
        {
            BoardDefinitionDto board = CreateSimpleBoard(20);

            var logic = new GameplayLogic(
                board,
                new[] { USER1_ID, USER2_ID });

            TurnTimeoutResult result = logic.HandleTurnTimeout();

            bool isOk =
                result != null &&
                result.PreviousTurnUserId == USER1_ID &&
                result.CurrentTurnUserId == USER2_ID &&
                !result.PlayerKicked &&
                !result.GameFinished;

            Assert.True(isOk);
        }

        [Fact]
        public void TestHandleTurnTimeoutEventuallyKicksPlayerAndMayFinishGame()
        {
            BoardDefinitionDto board = CreateSimpleBoard(20);

            var logic = new GameplayLogic(
                board,
                new[] { USER1_ID, USER2_ID });

            TurnTimeoutResult lastResult = null;

            const int MAX_ITERATIONS = 20;

            for (int index = 0; index < MAX_ITERATIONS; index++)
            {
                lastResult = logic.HandleTurnTimeout();

                if (lastResult.GameFinished)
                {
                    break;
                }
            }

            bool isOk =
                lastResult != null &&
                lastResult.PlayerKicked &&
                lastResult.GameFinished &&
                lastResult.WinnerUserId > 0;

            Assert.True(isOk);
        }

        [Fact]
        public void TestHandleTurnTimeoutThrowsWhenGameAlreadyFinished()
        {
            BoardDefinitionDto board = CreateSimpleBoard(20);

            var logic = new GameplayLogic(
                board,
                new[] { USER1_ID, USER2_ID });

            TurnTimeoutResult lastResult = null;
            const int MAX_ITERATIONS = 20;

            for (int index = 0; index < MAX_ITERATIONS; index++)
            {
                lastResult = logic.HandleTurnTimeout();

                if (lastResult.GameFinished)
                {
                    break;
                }
            }

            var ex = Assert.Throws<InvalidOperationException>(
                () => logic.HandleTurnTimeout());

            bool isOk =
                ex.Message == ERROR_GAME_ALREADY_FINISHED_EN ||
                ex.Message == ERROR_NO_PLAYERS_EN;

            Assert.True(isOk);
        }



        [Fact]
        public void TestRollDiceMovesPlayerAndUpdatesSnapshotWhenNoSpecialCells()
        {
            BoardDefinitionDto board = CreateSimpleBoard(30);

            var logic = new GameplayLogic(
                board,
                new[] { USER1_ID, USER2_ID });

            GameStateSnapshot before = logic.GetCurrentState();

            int initialCell = before
                .Tokens
                .First(t => t.UserId == USER1_ID)
                .CellIndex;

            RollDiceResult rollResult = logic.RollDice(USER1_ID, null);

            GameStateSnapshot after = logic.GetCurrentState();

            int newCell = after
                .Tokens
                .First(t => t.UserId == USER1_ID)
                .CellIndex;

            bool isOk =
                rollResult != null &&
                rollResult.FromCellIndex == initialCell &&
                rollResult.ToCellIndex == newCell &&
                rollResult.DiceValue != 0 &&
                !rollResult.IsGameOver;

            Assert.True(isOk);
        }

        private static BoardDefinitionDto CreateSimpleBoard(int cellCount)
        {
            var cells = new List<BoardCellDto>();

            for (int index = 1; index <= cellCount; index++)
            {
                cells.Add(new BoardCellDto
                {
                    Index = index,
                    SpecialType = SpecialCellType.None
                });
            }

            return new BoardDefinitionDto
            {
                Cells = cells,
                Links = new List<BoardLinkDto>()
            };
        }

    }
}
