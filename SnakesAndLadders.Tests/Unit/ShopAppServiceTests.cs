using System;
using System.Collections.Generic;
using Moq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class ShopAppServiceTests : IDisposable
    {
        private const string VALID_TOKEN = "valid-token";
        private const int VALID_USER_ID = 10;

        private const string CUSTOM_ERROR_CODE = "SHOP_CUSTOM_ERROR";

        private readonly Mock<IShopRepository> _shopRepositoryMock;
        private readonly Mock<Func<string, int>> _getUserIdFromTokenMock;

        private readonly ShopAppService _service;

        public ShopAppServiceTests()
        {
            _shopRepositoryMock =
                new Mock<IShopRepository>(MockBehavior.Strict);

            _getUserIdFromTokenMock =
                new Mock<Func<string, int>>(MockBehavior.Strict);

            _service = new ShopAppService(
                _shopRepositoryMock.Object,
                _getUserIdFromTokenMock.Object);
        }

        public void Dispose()
        {
            _shopRepositoryMock.VerifyAll();
            _getUserIdFromTokenMock.VerifyAll();
        }

        #region Constructor

        [Fact]
        public void TestConstructorThrowsWhenShopRepositoryIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new ShopAppService(
                    null,
                    _getUserIdFromTokenMock.Object));

            Assert.Equal("shopRepository", ex.ParamName);
        }

        [Fact]
        public void TestConstructorThrowsWhenGetUserIdFromTokenIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new ShopAppService(
                    _shopRepositoryMock.Object,
                    null));

            Assert.Equal("getUserIdFromToken", ex.ParamName);
        }

        #endregion

        #region EnsureUser (invalid session)

        [Fact]
        public void TestPurchaseAvatarChestThrowsWhenUserIdIsInvalid()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(0);

            Exception ex = Record.Exception(
                () => _service.PurchaseAvatarChest(
                    VALID_TOKEN,
                    ShopChestRarity.Common));

            Assert.NotNull(ex);
        }

        #endregion

        #region PurchaseAvatarChest

        [Theory]
        [InlineData(ShopChestRarity.Common)]
        [InlineData(ShopChestRarity.Epic)]
        [InlineData(ShopChestRarity.Legendary)]
        public void TestPurchaseAvatarChestReturnsRewardWhenRepositorySucceeds(
            ShopChestRarity rarity)
        {
            var reward = new ShopRewardDto();
            AvatarChestPurchaseDto capturedRequest = null;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseAvatarChest(
                    It.IsAny<AvatarChestPurchaseDto>()))
                .Returns(OperationResult<ShopRewardDto>.Success(reward))
                .Callback<AvatarChestPurchaseDto>(dto => capturedRequest = dto);

            ShopRewardDto result = _service.PurchaseAvatarChest(
                VALID_TOKEN,
                rarity);

            Assert.Same(reward, result);
            Assert.NotNull(capturedRequest);
            Assert.Equal(VALID_USER_ID, capturedRequest.UserId);
            Assert.Equal(rarity, capturedRequest.Rarity);
            Assert.True(capturedRequest.PriceCoins > 0);
        }

        [Fact]
        public void TestPurchaseAvatarChestThrowsWhenResultIsNull()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseAvatarChest(
                    It.IsAny<AvatarChestPurchaseDto>()))
                .Returns((OperationResult<ShopRewardDto>)null);

            Exception ex = Record.Exception(
                () => _service.PurchaseAvatarChest(
                    VALID_TOKEN,
                    ShopChestRarity.Common));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestPurchaseAvatarChestThrowsWhenResultIsFailureWithCustomError()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<ShopRewardDto> repoResult =
                OperationResult<ShopRewardDto>.Failure(CUSTOM_ERROR_CODE);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseAvatarChest(
                    It.IsAny<AvatarChestPurchaseDto>()))
                .Returns(repoResult);

            Exception ex = Record.Exception(
                () => _service.PurchaseAvatarChest(
                    VALID_TOKEN,
                    ShopChestRarity.Common));

            Assert.NotNull(ex);
            Assert.Equal(CUSTOM_ERROR_CODE, ex.Message);
        }

        [Fact]
        public void TestPurchaseAvatarChestThrowsWhenRewardDataIsNull()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<ShopRewardDto> repoResult =
                OperationResult<ShopRewardDto>.Success(null);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseAvatarChest(
                    It.IsAny<AvatarChestPurchaseDto>()))
                .Returns(repoResult);

            Exception ex = Record.Exception(
                () => _service.PurchaseAvatarChest(
                    VALID_TOKEN,
                    ShopChestRarity.Common));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestPurchaseAvatarChestThrowsArgumentOutOfRangeWhenRarityUnsupported()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            var invalidRarity = (ShopChestRarity)255;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.PurchaseAvatarChest(
                    VALID_TOKEN,
                    invalidRarity));
        }

        #endregion

        #region PurchaseStickerChest

        [Theory]
        [InlineData(ShopChestRarity.Common)]
        [InlineData(ShopChestRarity.Epic)]
        [InlineData(ShopChestRarity.Legendary)]
        public void TestPurchaseStickerChestReturnsRewardWhenRepositorySucceeds(
            ShopChestRarity rarity)
        {
            var reward = new ShopRewardDto();
            StickerChestPurchaseDto capturedRequest = null;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseStickerChest(
                    It.IsAny<StickerChestPurchaseDto>()))
                .Returns(OperationResult<ShopRewardDto>.Success(reward))
                .Callback<StickerChestPurchaseDto>(dto => capturedRequest = dto);

            ShopRewardDto result = _service.PurchaseStickerChest(
                VALID_TOKEN,
                rarity);

            Assert.Same(reward, result);
            Assert.NotNull(capturedRequest);
            Assert.Equal(VALID_USER_ID, capturedRequest.UserId);
            Assert.Equal(rarity, capturedRequest.Rarity);
            Assert.True(capturedRequest.PriceCoins > 0);
        }

        [Fact]
        public void TestPurchaseStickerChestThrowsArgumentOutOfRangeWhenRarityUnsupported()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            var invalidRarity = (ShopChestRarity)255;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => _service.PurchaseStickerChest(
                    VALID_TOKEN,
                    invalidRarity));
        }

        #endregion

        #region PurchaseDice

        [Fact]
        public void TestPurchaseDiceReturnsRewardWhenRepositorySucceeds()
        {
            const int diceId = 5;
            var reward = new ShopRewardDto();
            DicePurchaseDto capturedRequest = null;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseDice(
                    It.IsAny<DicePurchaseDto>()))
                .Returns(OperationResult<ShopRewardDto>.Success(reward))
                .Callback<DicePurchaseDto>(dto => capturedRequest = dto);

            ShopRewardDto result = _service.PurchaseDice(
                VALID_TOKEN,
                diceId);

            Assert.Same(reward, result);
            Assert.NotNull(capturedRequest);
            Assert.Equal(VALID_USER_ID, capturedRequest.UserId);
            Assert.Equal(diceId, capturedRequest.DiceId);
            Assert.True(capturedRequest.PriceCoins > 0);
        }

        #endregion

        #region PurchaseItemChest

        [Fact]
        public void TestPurchaseItemChestReturnsRewardWhenRepositorySucceeds()
        {
            var reward = new ShopRewardDto();
            ItemChestPurchaseDto capturedRequest = null;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.PurchaseItemChest(
                    It.IsAny<ItemChestPurchaseDto>()))
                .Returns(OperationResult<ShopRewardDto>.Success(reward))
                .Callback<ItemChestPurchaseDto>(dto => capturedRequest = dto);

            ShopRewardDto result = _service.PurchaseItemChest(VALID_TOKEN);

            Assert.Same(reward, result);
            Assert.NotNull(capturedRequest);
            Assert.Equal(VALID_USER_ID, capturedRequest.UserId);
            Assert.True(capturedRequest.PriceCoins > 0);
        }

        #endregion

        #region GetCurrentCoins

        [Fact]
        public void TestGetCurrentCoinsReturnsBalanceWhenRepositorySucceeds()
        {
            const int expectedCoins = 123;

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<int> repoResult =
                OperationResult<int>.Success(expectedCoins);

            _shopRepositoryMock
                .Setup(repo => repo.GetCurrentCoins(VALID_USER_ID))
                .Returns(repoResult);

            int result = _service.GetCurrentCoins(VALID_TOKEN);

            Assert.Equal(expectedCoins, result);
        }

        [Fact]
        public void TestGetCurrentCoinsThrowsWhenResultIsNull()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.GetCurrentCoins(VALID_USER_ID))
                .Returns((OperationResult<int>)null);

            Exception ex = Record.Exception(
                () => _service.GetCurrentCoins(VALID_TOKEN));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestGetCurrentCoinsThrowsWithCustomErrorWhenResultIsFailure()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<int> repoResult =
                OperationResult<int>.Failure(CUSTOM_ERROR_CODE);

            _shopRepositoryMock
                .Setup(repo => repo.GetCurrentCoins(VALID_USER_ID))
                .Returns(repoResult);

            Exception ex = Record.Exception(
                () => _service.GetCurrentCoins(VALID_TOKEN));

            Assert.NotNull(ex);
            Assert.Equal(CUSTOM_ERROR_CODE, ex.Message);
        }

        #endregion

        #region GetUserStickers

        [Fact]
        public void TestGetUserStickersReturnsListWhenRepositorySucceeds()
        {
            var list = new List<StickerDto>
            {
                new StickerDto(),
                new StickerDto()
            };

            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<List<StickerDto>> repoResult =
                OperationResult<List<StickerDto>>.Success(list);

            _shopRepositoryMock
                .Setup(repo => repo.GetUserStickers(VALID_USER_ID))
                .Returns(repoResult);

            List<StickerDto> result = _service.GetUserStickers(VALID_TOKEN);

            Assert.Same(list, result);
        }

        [Fact]
        public void TestGetUserStickersThrowsWhenResultIsNull()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            _shopRepositoryMock
                .Setup(repo => repo.GetUserStickers(VALID_USER_ID))
                .Returns((OperationResult<List<StickerDto>>)null);

            Exception ex = Record.Exception(
                () => _service.GetUserStickers(VALID_TOKEN));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestGetUserStickersThrowsWithCustomErrorWhenResultIsFailure()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<List<StickerDto>> repoResult =
                OperationResult<List<StickerDto>>.Failure(CUSTOM_ERROR_CODE);

            _shopRepositoryMock
                .Setup(repo => repo.GetUserStickers(VALID_USER_ID))
                .Returns(repoResult);

            Exception ex = Record.Exception(
                () => _service.GetUserStickers(VALID_TOKEN));

            Assert.NotNull(ex);
            Assert.Equal(CUSTOM_ERROR_CODE, ex.Message);
        }

        [Fact]
        public void TestGetUserStickersThrowsWhenDataIsNull()
        {
            _getUserIdFromTokenMock
                .Setup(f => f(VALID_TOKEN))
                .Returns(VALID_USER_ID);

            OperationResult<List<StickerDto>> repoResult =
                OperationResult<List<StickerDto>>.Success(null);

            _shopRepositoryMock
                .Setup(repo => repo.GetUserStickers(VALID_USER_ID))
                .Returns(repoResult);

            Exception ex = Record.Exception(
                () => _service.GetUserStickers(VALID_TOKEN));

            Assert.NotNull(ex);
        }

        #endregion
    }
}
