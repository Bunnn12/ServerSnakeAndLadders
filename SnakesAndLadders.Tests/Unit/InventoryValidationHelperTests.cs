using System;
using SnakesAndLadders.Services.Logic.Inventory;
using Xunit;
using static SnakesAndLadders.Services.Constants.InventoryAppServiceConstants;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class InventoryValidationHelperTests
    {


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(MIN_VALID_USER_ID - 1)]
        public void TestValidateUserIdThrowsWhenLessThanMin(int userId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => InventoryValidationHelper.ValidateUserId(userId));

            bool isOk =
                ex.ParamName == "userId" &&
                ex.Message.Contains(ERROR_USER_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(MIN_VALID_USER_ID)]
        [InlineData(MIN_VALID_USER_ID + 10)]
        public void TestValidateUserIdDoesNotThrowWhenValid(int userId)
        {
            InventoryValidationHelper.ValidateUserId(userId);

            bool isOk = true;

            Assert.True(isOk);
        }



        [Theory]
        [InlineData((byte)(MIN_ITEM_SLOT - 1))]
        [InlineData((byte)(MAX_ITEM_SLOT + 1))]
        public void TestValidateItemSlotThrowsWhenOutOfRange(byte slotNumber)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => InventoryValidationHelper.ValidateItemSlot(slotNumber));

            bool isOk =
                ex.ParamName == "slotNumber" &&
                ex.Message.Contains(ERROR_ITEM_SLOT_RANGE);

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(MIN_ITEM_SLOT)]
        [InlineData(MAX_ITEM_SLOT)]
        public void TestValidateItemSlotDoesNotThrowWhenInRange(byte slotNumber)
        {
            InventoryValidationHelper.ValidateItemSlot(slotNumber);

            bool isOk = true;

            Assert.True(isOk);
        }



        [Theory]
        [InlineData((byte)(MIN_DICE_SLOT - 1))]
        [InlineData((byte)(MAX_DICE_SLOT + 1))]
        public void TestValidateDiceSlotThrowsWhenOutOfRange(byte slotNumber)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => InventoryValidationHelper.ValidateDiceSlot(slotNumber));

            bool isOk =
                ex.ParamName == "slotNumber" &&
                ex.Message.Contains(ERROR_DICE_SLOT_RANGE);

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(MIN_DICE_SLOT)]
        [InlineData(MAX_DICE_SLOT)]
        public void TestValidateDiceSlotDoesNotThrowWhenInRange(byte slotNumber)
        {
            InventoryValidationHelper.ValidateDiceSlot(slotNumber);

            bool isOk = true;

            Assert.True(isOk);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestValidateObjectIdThrowsWhenNonPositive(int objectId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => InventoryValidationHelper.ValidateObjectId(objectId));

            bool isOk =
                ex.ParamName == "objectId" &&
                ex.Message.Contains(ERROR_OBJECT_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void TestValidateObjectIdDoesNotThrowWhenPositive(int objectId)
        {
            InventoryValidationHelper.ValidateObjectId(objectId);

            bool isOk = true;

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestValidateDiceIdThrowsWhenNonPositive(int diceId)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => InventoryValidationHelper.ValidateDiceId(diceId));

            bool isOk =
                ex.ParamName == "diceId" &&
                ex.Message.Contains(ERROR_DICE_ID_POSITIVE);

            Assert.True(isOk);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(20)]
        public void TestValidateDiceIdDoesNotThrowWhenPositive(int diceId)
        {
            InventoryValidationHelper.ValidateDiceId(diceId);

            bool isOk = true;

            Assert.True(isOk);
        }


    }
}
