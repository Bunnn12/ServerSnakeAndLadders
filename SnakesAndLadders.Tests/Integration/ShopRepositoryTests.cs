using System;
using System.Linq;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Repositories;
using SnakesAndLadders.Tests.integration;
using Xunit;

namespace SnakesAndLadders.Tests.Integration
{
    public sealed class ShopRepositoryTests : IntegrationTestBase
    {
        private const int INVALID_USER_ID_ZERO = 0;
        private const int INVALID_USER_ID_NEGATIVE = -1;

        private const int USER_COINS_LOW = 5;
        private const int USER_COINS_HIGH = 1_000;

        private const byte STATUS_ACTIVE = 0x01;

        private const string BASE_USERNAME = "ShopUser";
        private const string BASE_FIRST_NAME = "Shop";
        private const string BASE_LAST_NAME = "User";

        private ShopRepository CreateRepository()
        {
            return new ShopRepository(CreateContext);
        }

        private int CreateUserWithCoins(int coins)
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);

                Usuario user = new Usuario
                {
                    NombreUsuario = $"{BASE_USERNAME}_{suffix}",
                    Nombre = BASE_FIRST_NAME,
                    Apellidos = BASE_LAST_NAME,
                    DescripcionPerfil = null,
                    FotoPerfil = null,
                    Monedas = coins,
                    Estado = new[] { STATUS_ACTIVE },
                    IdAvatarDesbloqueadoActual = null
                };

                db.Usuario.Add(user);
                db.SaveChanges();

                return user.IdUsuario;
            }
        }

        // PurchaseAvatarChest

        [Fact]
        public void TestPurchaseAvatarChestWhenRequestIsNullReturnsFailureResult()
        {
            ShopRepository repository = CreateRepository();

            OperationResult<ShopRewardDto> result = repository.PurchaseAvatarChest(null);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestPurchaseAvatarChestWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            AvatarChestPurchaseDto request = new AvatarChestPurchaseDto
            {
                UserId = invalidUserId,
                PriceCoins = 10,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseAvatarChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseAvatarChestWhenUserNotFoundReturnsFailureResult()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            AvatarChestPurchaseDto request = new AvatarChestPurchaseDto
            {
                UserId = ShopRulesRepository.MIN_USER_ID,
                PriceCoins = 10,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseAvatarChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseAvatarChestWhenUserHasInsufficientCoinsReturnsFailureResult()
        {
            int userId = CreateUserWithCoins(USER_COINS_LOW);

            ShopRepository repository = CreateRepository();

            AvatarChestPurchaseDto request = new AvatarChestPurchaseDto
            {
                UserId = userId,
                PriceCoins = USER_COINS_LOW + 1,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseAvatarChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        // PurchaseStickerChest

        [Fact]
        public void TestPurchaseStickerChestWhenRequestIsNullReturnsFailureResult()
        {
            ShopRepository repository = CreateRepository();

            OperationResult<ShopRewardDto> result = repository.PurchaseStickerChest(null);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestPurchaseStickerChestWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            StickerChestPurchaseDto request = new StickerChestPurchaseDto
            {
                UserId = invalidUserId,
                PriceCoins = 10,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseStickerChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseStickerChestWhenUserNotFoundReturnsFailureResult()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            StickerChestPurchaseDto request = new StickerChestPurchaseDto
            {
                UserId = ShopRulesRepository.MIN_USER_ID,
                PriceCoins = 10,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseStickerChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseStickerChestWhenUserHasInsufficientCoinsReturnsFailureResult()
        {
            int userId = CreateUserWithCoins(USER_COINS_LOW);

            ShopRepository repository = CreateRepository();

            StickerChestPurchaseDto request = new StickerChestPurchaseDto
            {
                UserId = userId,
                PriceCoins = USER_COINS_LOW + 1,
                Rarity = ShopChestRarity.Common
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseStickerChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        // PurchaseDice

        [Fact]
        public void TestPurchaseDiceWhenRequestIsNullReturnsFailureResult()
        {
            ShopRepository repository = CreateRepository();

            OperationResult<ShopRewardDto> result = repository.PurchaseDice(null);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestPurchaseDiceWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            DicePurchaseDto request = new DicePurchaseDto
            {
                UserId = invalidUserId,
                DiceId = 1,
                PriceCoins = 10
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseDice(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TestPurchaseDiceWhenDiceIdLessThanMinReturnsFailureResult(int invalidDiceId)
        {
            int userId = CreateUserWithCoins(USER_COINS_HIGH);

            ShopRepository repository = CreateRepository();

            DicePurchaseDto request = new DicePurchaseDto
            {
                UserId = userId,
                DiceId = invalidDiceId,
                PriceCoins = 10
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseDice(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseDiceWhenUserNotFoundReturnsFailureResult()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            DicePurchaseDto request = new DicePurchaseDto
            {
                UserId = ShopRulesRepository.MIN_USER_ID,
                DiceId = 1,
                PriceCoins = 10
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseDice(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseDiceWhenUserHasInsufficientCoinsReturnsFailureResult()
        {
            int userId = CreateUserWithCoins(USER_COINS_LOW);

            ShopRepository repository = CreateRepository();

            DicePurchaseDto request = new DicePurchaseDto
            {
                UserId = userId,
                DiceId = 1,
                PriceCoins = USER_COINS_LOW + 1
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseDice(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        // PurchaseItemChest

        [Fact]
        public void TestPurchaseItemChestWhenRequestIsNullReturnsFailureResult()
        {
            ShopRepository repository = CreateRepository();

            OperationResult<ShopRewardDto> result = repository.PurchaseItemChest(null);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestPurchaseItemChestWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            ItemChestPurchaseDto request = new ItemChestPurchaseDto
            {
                UserId = invalidUserId,
                PriceCoins = 10
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseItemChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseItemChestWhenUserNotFoundReturnsFailureResult()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            ItemChestPurchaseDto request = new ItemChestPurchaseDto
            {
                UserId = ShopRulesRepository.MIN_USER_ID,
                PriceCoins = 10
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseItemChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestPurchaseItemChestWhenUserHasInsufficientCoinsReturnsFailureResult()
        {
            int userId = CreateUserWithCoins(USER_COINS_LOW);

            ShopRepository repository = CreateRepository();

            ItemChestPurchaseDto request = new ItemChestPurchaseDto
            {
                UserId = userId,
                PriceCoins = USER_COINS_LOW + 1
            };

            OperationResult<ShopRewardDto> result = repository.PurchaseItemChest(request);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        // GetCurrentCoins

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetCurrentCoinsWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            OperationResult<int> result = repository.GetCurrentCoins(invalidUserId);

            bool isOk = result != null && result.Data == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetCurrentCoinsWhenUserNotFoundReturnsFailureResult()
        {
            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.Usuario.RemoveRange(db.Usuario);
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            OperationResult<int> result = repository.GetCurrentCoins(ShopRulesRepository.MIN_USER_ID);

            bool isOk = result != null && result.Data == 0;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetCurrentCoinsWhenUserExistsReturnsExpectedCoins()
        {
            int expectedCoins = USER_COINS_HIGH;
            int userId = CreateUserWithCoins(expectedCoins);

            ShopRepository repository = CreateRepository();

            OperationResult<int> result = repository.GetCurrentCoins(userId);

            bool isOk = result != null && result.Data == expectedCoins;
            Assert.True(isOk);
        }

        // GetUserStickers

        [Theory]
        [InlineData(INVALID_USER_ID_ZERO)]
        [InlineData(INVALID_USER_ID_NEGATIVE)]
        public void TestGetUserStickersWhenUserIdLessThanMinReturnsFailureResult(int invalidUserId)
        {
            ShopRepository repository = CreateRepository();

            OperationResult<System.Collections.Generic.List<StickerDto>> result =
                repository.GetUserStickers(invalidUserId);

            bool isOk = result != null && result.Data == null;
            Assert.True(isOk);
        }

        [Fact]
        public void TestGetUserStickersWhenUserHasNoPacksReturnsEmptyList()
        {
            int userId = CreateUserWithCoins(USER_COINS_HIGH);

            using (SnakeAndLaddersDBEntities1 db = CreateContext())
            {
                db.StickersUsuario.RemoveRange(db.StickersUsuario.Where(su => su.UsuarioIdUsuario == userId));
                db.SaveChanges();
            }

            ShopRepository repository = CreateRepository();

            OperationResult<System.Collections.Generic.List<StickerDto>> result =
                repository.GetUserStickers(userId);

            bool isOk =
                result != null &&
                result.Data != null &&
                !result.Data.Any();

            Assert.True(isOk);
        }
    }
}
