using ServerSnakesAndLadders;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakesAndLadders.Tests.integration;
using System;
using System.Collections.Generic;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class InventoryRepositoryTests : IntegrationTestBase
    {
        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const int VALID_USER_ID = 1;

        private const int INVALID_OBJECT_ID_ZERO = 0;
        private const int INVALID_OBJECT_ID_NEGATIVE = -1;

        private const int INVALID_DICE_ID_ZERO = 0;
        private const int INVALID_DICE_ID_NEGATIVE = -1;

        private InventoryRepository CreateRepository()
        {
            return new InventoryRepository(CreateContext);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetUserItemsWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GetUserItems(invalidUserId);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestGetUserItemsWhenUserHasNoItemsReturnsEmptyList()
        {
            InventoryRepository repository = CreateRepository();

            IList<InventoryItemDto> result = repository.GetUserItems(VALID_USER_ID);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetUserDiceWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GetUserDice(invalidUserId);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestGetUserDiceWhenUserHasNoDiceReturnsEmptyList()
        {
            InventoryRepository repository = CreateRepository();

            IList<InventoryDiceDto> result = repository.GetUserDice(VALID_USER_ID);

            bool isOk = result != null && result.Count == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestUpdateSelectedItemsWhenRequestIsNullThrowsArgumentNullException()
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.UpdateSelectedItems(null);

            bool throws = Assert.Throws<ArgumentNullException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestUpdateSelectedItemsWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            var request = new UpdateItemSlotsRequest
            {
                UserId = invalidUserId
            };

            Action action = () => repository.UpdateSelectedItems(request);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestUpdateSelectedDiceWhenRequestIsNullThrowsArgumentNullException()
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.UpdateSelectedDice(null);

            bool throws = Assert.Throws<ArgumentNullException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestUpdateSelectedDiceWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            var request = new UpdateDiceSlotsRequest
            {
                UserId = invalidUserId
            };

            Action action = () => repository.UpdateSelectedDice(request);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestRemoveItemFromSlotWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.RemoveItemFromSlot(invalidUserId, 1);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)4)]
        public void TestRemoveItemFromSlotWhenSlotNumberInvalidThrowsArgumentOutOfRange(
            byte invalidSlot)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.RemoveItemFromSlot(VALID_USER_ID, invalidSlot);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestRemoveDiceFromSlotWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.RemoveDiceFromSlot(invalidUserId, 1);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)3)]
        public void TestRemoveDiceFromSlotWhenSlotNumberInvalidThrowsArgumentOutOfRange(
            byte invalidSlot)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.RemoveDiceFromSlot(VALID_USER_ID, invalidSlot);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestConsumeItemWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeItem(invalidUserId, 1);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_OBJECT_ID_ZERO)]
        [InlineData(INVALID_OBJECT_ID_NEGATIVE)]
        public void TestConsumeItemWhenObjectIdNotPositiveThrowsArgumentOutOfRange(
            int invalidObjectId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeItem(VALID_USER_ID, invalidObjectId);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestConsumeItemWhenUserDoesNotOwnItemThrowsInvalidOperationException()
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeItem(VALID_USER_ID, 999);

            bool throws = Assert.Throws<InvalidOperationException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestConsumeDiceWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeDice(invalidUserId, 1);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_DICE_ID_ZERO)]
        [InlineData(INVALID_DICE_ID_NEGATIVE)]
        public void TestConsumeDiceWhenDiceIdNotPositiveThrowsArgumentOutOfRange(
            int invalidDiceId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeDice(VALID_USER_ID, invalidDiceId);

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestConsumeDiceWhenUserDoesNotOwnDiceThrowsInvalidOperationException()
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.ConsumeDice(VALID_USER_ID, 999);

            bool throws = Assert.Throws<InvalidOperationException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGrantItemToUserWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GrantItemToUser(invalidUserId, "ITEM1");

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestGrantItemToUserWhenItemCodeNullOrWhiteSpaceThrowsArgumentException(
            string invalidCode)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GrantItemToUser(VALID_USER_ID, invalidCode);

            bool throws = Assert.Throws<ArgumentException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestGrantItemToUserWhenItemConfigNotFoundReturnsFailure()
        {
            InventoryRepository repository = CreateRepository();

            OperationResult<bool> result = repository.GrantItemToUser(
                VALID_USER_ID,
                "UNKNOWN_ITEM");

            bool isOk = result != null && !result.IsSuccess;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGrantDiceToUserWhenUserIdNotPositiveThrowsArgumentOutOfRange(
            int invalidUserId)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GrantDiceToUser(invalidUserId, "DICE1");

            bool throws = Assert.Throws<ArgumentOutOfRangeException>(action) != null;
            Assert.True(throws);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestGrantDiceToUserWhenDiceCodeNullOrWhiteSpaceThrowsArgumentException(
            string invalidCode)
        {
            InventoryRepository repository = CreateRepository();

            Action action = () => repository.GrantDiceToUser(VALID_USER_ID, invalidCode);

            bool throws = Assert.Throws<ArgumentException>(action) != null;
            Assert.True(throws);
        }

        [Fact]
        public void TestGrantDiceToUserWhenDiceConfigNotFoundReturnsFailure()
        {
            InventoryRepository repository = CreateRepository();

            OperationResult<bool> result = repository.GrantDiceToUser(
                VALID_USER_ID,
                "UNKNOWN_DICE");

            bool isOk = result != null && !result.IsSuccess;
            Assert.True(isOk);
        }
    }
}
