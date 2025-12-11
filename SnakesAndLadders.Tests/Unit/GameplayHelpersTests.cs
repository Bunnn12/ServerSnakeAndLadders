using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class GameplayHelpersTests
    {
        private const int USER_ID = 10;

        private const string DICE_CODE_NEGATIVE = "DICE_NEG";
        private const string DICE_CODE_123 = "DICE_123";
        private const string DICE_CODE_456 = "DICE_456";


        [Fact]
        public void TestDiceManagerConstructorThrowsWhenRandomIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new DiceManager(null));

            bool isOk = ex.ParamName == "random";

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueThrowsWhenPlayerStateIsNull()
        {
            var manager = new DiceManager(new Random(123));

            var ex = Assert.Throws<ArgumentNullException>(
                () => manager.GetDiceValue(null, null));

            bool isOk = ex.ParamName == "playerState";

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueDefaultRollIsBetweenOneAndSix()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 1
            };

            int value = manager.GetDiceValue(player, null);

            bool isOk = value >= 1 && value <= 6;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueNegativeDiceThrowsWhenTooCloseToStart()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 1
            };

            var ex = Assert.Throws<InvalidOperationException>(
                () => manager.GetDiceValue(player, DICE_CODE_NEGATIVE));

            bool isOk =
                !string.IsNullOrWhiteSpace(ex.Message) &&
                ex.Message.Contains("dado negativo");

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueNegativeDiceReturnsNegativeValueInRangeWhenAllowed()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 10
            };

            int value = manager.GetDiceValue(player, DICE_CODE_NEGATIVE);

            bool isOk = value <= -1 && value >= -6;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueOneTwoThreeReturnsValueBetweenOneAndThree()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 5
            };

            int value = manager.GetDiceValue(player, DICE_CODE_123);

            bool isOk = value >= 1 && value <= 3;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueFourFiveSixReturnsValueBetweenFourAndSix()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 5
            };

            int value = manager.GetDiceValue(player, DICE_CODE_456);

            bool isOk = value >= 4 && value <= 6;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetDiceValueUnknownCodeBehavesLikeNormalDice()
        {
            var manager = new DiceManager(new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 5
            };

            int value = manager.GetDiceValue(player, "DICE_UNKNOWN");

            bool isOk = value >= 1 && value <= 6;

            Assert.True(isOk);
        }


        [Fact]
        public void TestApplyJumpEffectsIfAnyAppliesLadder()
        {
            BoardDefinitionDto board = CreateBoardWithJumps();
            var navigator = new BoardNavigator(board, new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 5
            };

            int result = navigator.ApplyJumpEffectsIfAny(player, 5);

            bool isOk = result == 10;

            Assert.True(isOk);
        }

        [Fact]
        public void TestApplyJumpEffectsIfAnyAppliesSnakeWhenNoShield()
        {
            BoardDefinitionDto board = CreateBoardWithJumps();
            var navigator = new BoardNavigator(board, new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 8,
                HasShield = false
            };

            int result = navigator.ApplyJumpEffectsIfAny(player, 8);

            bool isOk = result == 3;

            Assert.True(isOk);
        }

        [Fact]
        public void TestApplyJumpEffectsIfAnyDoesNotApplySnakeWhenShield()
        {
            BoardDefinitionDto board = CreateBoardWithJumps();
            var navigator = new BoardNavigator(board, new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 8,
                HasShield = true
            };

            int result = navigator.ApplyJumpEffectsIfAny(player, 8);

            bool isOk = result == 8;

            Assert.True(isOk);
        }


        [Fact]
        public void TestApplySpecialCellIfAnyItemCellGrantsItem()
        {
            BoardDefinitionDto board = CreateBoardWithSpecialCells();
            var navigator = new BoardNavigator(board, new Random(123));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 2
            };

            var result = navigator.ApplySpecialCellIfAny(
                player,
                2,
                string.Empty);

            bool isOk =
                result.FinalCellIndex == 2 &&
                !string.IsNullOrWhiteSpace(result.GrantedItemCode) &&
                !string.IsNullOrWhiteSpace(result.ExtraInfo);

            Assert.True(isOk);
        }

        [Fact]
        public void TestApplySpecialCellIfAnyDiceCellGrantsDice()
        {
            BoardDefinitionDto board = CreateBoardWithSpecialCells();
            var navigator = new BoardNavigator(board, new Random(456));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 3
            };

            var result = navigator.ApplySpecialCellIfAny(
                player,
                3,
                string.Empty);

            bool isOk =
                result.FinalCellIndex == 3 &&
                !string.IsNullOrWhiteSpace(result.GrantedDiceCode) &&
                !string.IsNullOrWhiteSpace(result.ExtraInfo);

            Assert.True(isOk);
        }

        [Fact]
        public void TestApplyMessageCellEffectSetsMessageIndexAndStaysWithinBoardBounds()
        {
            BoardDefinitionDto board = CreateBoardWithSpecialCells();
            var navigator = new BoardNavigator(board, new Random(789));

            var player = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 4
            };

            var result = navigator.ApplySpecialCellIfAny(
                player,
                4,
                string.Empty);

            bool isOk =
                result.MessageIndex.HasValue &&
                result.MessageIndex.Value >= 1 &&
                result.MessageIndex.Value <= 20 &&
                result.FinalCellIndex >= 1 &&
                result.FinalCellIndex <= navigator.FinalCellIndex &&
                !string.IsNullOrWhiteSpace(result.ExtraInfo);

            Assert.True(isOk);
        }


        [Fact]
        public void TestThrowGameAlreadyFinishedAlwaysThrowsInvalidOperation()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.ThrowGameAlreadyFinished());

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureThereArePlayersThrowsWhenListIsNullOrEmpty()
        {
            var ex1 = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.EnsureThereArePlayers(null));

            var ex2 = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.EnsureThereArePlayers(
                    new List<int>()));

            bool isOk =
                !string.IsNullOrWhiteSpace(ex1.Message) &&
                !string.IsNullOrWhiteSpace(ex2.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureThereArePlayersDoesNotThrowWhenListHasPlayers()
        {
            var players = new List<int> { USER_ID };

            GameplayValidationHelper.EnsureThereArePlayers(players);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureUserInGameThrowsWhenUserNotInList()
        {
            var players = new List<int> { 1, 2, 3 };

            var ex = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.EnsureUserInGame(players, 99));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureUserInGameDoesNotThrowWhenUserExists()
        {
            var players = new List<int> { 1, 2, 3 };

            GameplayValidationHelper.EnsureUserInGame(players, 2);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureIsUserTurnThrowsWhenListIsEmpty()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.EnsureIsUserTurn(
                    new List<int>(),
                    0,
                    USER_ID));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureIsUserTurnThrowsWhenNotUserTurn()
        {
            var players = new List<int> { 1, 2 };

            var ex = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.EnsureIsUserTurn(
                    players,
                    0,
                    2));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }

        [Fact]
        public void TestEnsureIsUserTurnDoesNotThrowWhenIsUserTurn()
        {
            var players = new List<int> { 1, 2 };

            GameplayValidationHelper.EnsureIsUserTurn(
                players,
                1,
                2);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetPlayerStateOrThrowReturnsStateWhenExists()
        {
            var state = new PlayerRuntimeState
            {
                UserId = USER_ID,
                Position = 5
            };

            var dict = new Dictionary<int, PlayerRuntimeState>
            {
                { USER_ID, state }
            };

            PlayerRuntimeState result =
                GameplayValidationHelper.GetPlayerStateOrThrow(
                    dict,
                    USER_ID);

            bool isOk = ReferenceEquals(state, result);

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetPlayerStateOrThrowThrowsWhenUserNotFound()
        {
            var dict = new Dictionary<int, PlayerRuntimeState>();

            var ex = Assert.Throws<InvalidOperationException>(
                () => GameplayValidationHelper.GetPlayerStateOrThrow(
                    dict,
                    USER_ID));

            bool isOk = !string.IsNullOrWhiteSpace(ex.Message);

            Assert.True(isOk);
        }


        private static BoardDefinitionDto CreateBoardWithJumps()
        {
            var cells = new List<BoardCellDto>();

            for (int index = 1; index <= 10; index++)
            {
                cells.Add(new BoardCellDto
                {
                    Index = index,
                    SpecialType = SpecialCellType.None
                });
            }

            var links = new List<BoardLinkDto>
            {
                new BoardLinkDto
                {
                    StartIndex = 5,
                    EndIndex = 10   
                },
                new BoardLinkDto
                {
                    StartIndex = 8,
                    EndIndex = 3    
                }
            };

            return new BoardDefinitionDto
            {
                Cells = cells,
                Links = links
            };
        }

        private static BoardDefinitionDto CreateBoardWithSpecialCells()
        {
            var cells = new List<BoardCellDto>
            {
                new BoardCellDto { Index = 1, SpecialType = SpecialCellType.None },
                new BoardCellDto { Index = 2, SpecialType = SpecialCellType.Item },
                new BoardCellDto { Index = 3, SpecialType = SpecialCellType.Dice },
                new BoardCellDto { Index = 4, SpecialType = SpecialCellType.Message },
                new BoardCellDto { Index = 5, SpecialType = SpecialCellType.None }
            };

            return new BoardDefinitionDto
            {
                Cells = cells,
                Links = new List<BoardLinkDto>()
            };
        }

    }
}
