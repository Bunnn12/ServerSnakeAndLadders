using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ShopRepository : IShopRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ShopRepository));
        private static readonly Random RandomInstance = new Random();
        private const byte STATUS_ACTIVE = 0x01;

        private readonly Func<SnakeAndLaddersDBEntities1> contextFactory;

        public ShopRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            this.contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public OperationResult<ShopRewardDto> PurchaseAvatarChest(AvatarChestPurchaseDto request)
        {
            if (request == null)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }

            if (request.UserId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == request.UserId);
                        if (user == null)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_USER_NOT_FOUND);
                        }

                        if (user.Monedas < request.PriceCoins)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INSUFFICIENT_COINS);
                        }

                        string rarityText = request.Rarity.ToString();

                        List<int> unlockedIds = context.AvatarDesbloqueado
                            .Where(a => a.UsuarioIdUsuario == request.UserId)
                            .Select(a => a.AvatarIdAvatar)
                            .ToList();

                        List<Avatar> candidates = context.Avatar
                            .Where(a => a.RarezaAvatar == rarityText)
                            .ToList();

                        if (!candidates.Any())
                        {
                            Logger.WarnFormat("No avatar candidates found for rarity {0}.", rarityText);
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_NO_AVATARS_FOR_RARITY);
                        }

                        List<Avatar> notOwned = candidates
                            .Where(a => !unlockedIds.Contains(a.IdAvatar))
                            .ToList();

                        List<Avatar> selectionPool = notOwned.Any() ? notOwned : candidates;
                        Avatar selectedAvatar = GetRandomAvatar(selectionPool);

                        bool isNewForUser = !unlockedIds.Contains(selectedAvatar.IdAvatar);

                        int coinsBefore = user.Monedas;
                        user.Monedas = user.Monedas - request.PriceCoins;

                        if (isNewForUser)
                        {
                            AvatarDesbloqueado unlocked = new AvatarDesbloqueado
                            {
                                AvatarIdAvatar = selectedAvatar.IdAvatar,
                                UsuarioIdUsuario = request.UserId,
                                FechaDesbloqueo = DateTime.UtcNow.Date
                            };

                            context.AvatarDesbloqueado.Add(unlocked);
                        }

                        context.SaveChanges();
                        transaction.Commit();

                        ShopRewardDto reward = new ShopRewardDto
                        {
                            RewardType = ShopRewardType.Avatar,
                            RewardId = selectedAvatar.IdAvatar,
                            RewardCode = selectedAvatar.CodigoAvatar,
                            RewardName = selectedAvatar.NombreAvatar,
                            IsNewForUser = isNewForUser,
                            CoinsBefore = coinsBefore,
                            CoinsAfter = user.Monedas
                        };

                        return OperationResult<ShopRewardDto>.Success(reward);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("SQL error while purchasing avatar chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_DB);
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("EF error while purchasing avatar chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Unexpected error while purchasing avatar chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error creating EF context for avatar chest.", ex);
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_FATAL);
            }
        }

        private static ShopChestRarity GetStickerPackRarity(string packCode)
        {
            if (string.IsNullOrWhiteSpace(packCode))
            {
                return ShopChestRarity.Common;
            }

            switch (packCode.ToUpperInvariant())
            {
                case ShopRulesRepository.STICKER_PACK_SAUL:
                    return ShopChestRarity.Epic;

                case ShopRulesRepository.STICKER_PACK_LIZ:
                    return ShopChestRarity.Epic;

                case ShopRulesRepository.STICKER_PACK_REVO:
                case ShopRulesRepository.STICKER_PACK_OCHARAN:
                    return ShopChestRarity.Legendary;

                default:
                    return ShopChestRarity.Common;
            }
        }

        public OperationResult<ShopRewardDto> PurchaseStickerChest(StickerChestPurchaseDto request)
        {
            if (request == null)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }

            if (request.UserId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == request.UserId);
                        if (user == null)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_USER_NOT_FOUND);
                        }

                        if (user.Monedas < request.PriceCoins)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INSUFFICIENT_COINS);
                        }

                        List<int> ownedPackIds = context.StickersUsuario
                            .Where(su => su.UsuarioIdUsuario == request.UserId)
                            .Select(su => su.PaqueteStickersIdPaqueteStickers)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .ToList();

                        List<PaqueteStickers> allPacks = context.PaqueteStickers.ToList();

                        List<PaqueteStickers> candidates = allPacks
                            .Where(p => GetStickerPackRarity(p.CodigoPaqueteStickers) == request.Rarity)
                            .ToList();

                        if (!candidates.Any())
                        {
                            Logger.WarnFormat("No sticker pack candidates found for rarity {0}.", request.Rarity);
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_NO_STICKER_PACKS);
                        }

                        List<PaqueteStickers> notOwned = candidates
                            .Where(p => !ownedPackIds.Contains(p.IdPaqueteStickers))
                            .ToList();

                        List<PaqueteStickers> selectionPool = notOwned.Any() ? notOwned : candidates;
                        PaqueteStickers selectedPack = GetRandomStickerPack(selectionPool);

                        bool isNewForUser = !ownedPackIds.Contains(selectedPack.IdPaqueteStickers);

                        int coinsBefore = user.Monedas;
                        user.Monedas = user.Monedas - request.PriceCoins;

                        if (isNewForUser)
                        {
                            StickersUsuario unlocked = new StickersUsuario
                            {
                                UsuarioIdUsuario = request.UserId,
                                PaqueteStickersIdPaqueteStickers = selectedPack.IdPaqueteStickers,
                                FechaDesbloqueo = DateTime.UtcNow
                            };

                            context.StickersUsuario.Add(unlocked);
                        }

                        context.SaveChanges();
                        transaction.Commit();

                        ShopRewardDto reward = new ShopRewardDto
                        {
                            RewardType = ShopRewardType.StickerPack,
                            RewardId = selectedPack.IdPaqueteStickers,
                            RewardCode = selectedPack.CodigoPaqueteStickers,
                            RewardName = selectedPack.Nombre,
                            IsNewForUser = isNewForUser,
                            CoinsBefore = coinsBefore,
                            CoinsAfter = user.Monedas
                        };

                        return OperationResult<ShopRewardDto>.Success(reward);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("SQL error while purchasing sticker chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_DB);
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("EF error while purchasing sticker chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Unexpected error while purchasing sticker chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error creating EF context for sticker chest.", ex);
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_FATAL);
            }
        }

        public OperationResult<ShopRewardDto> PurchaseDice(DicePurchaseDto request)
        {
            if (request == null)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }

            if (request.UserId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            if (request.DiceId < 1)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_DICE_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == request.UserId);
                        if (user == null)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_USER_NOT_FOUND);
                        }

                        if (user.Monedas < request.PriceCoins)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INSUFFICIENT_COINS);
                        }

                        Dado dice = context.Dado.SingleOrDefault(d => d.IdDado == request.DiceId);
                        if (dice == null)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_DICE_NOT_FOUND);
                        }

                        DadoUsuario userDice = context.DadoUsuario
                            .SingleOrDefault(
                                du => du.UsuarioIdUsuario == request.UserId &&
                                      du.DadoIdDado == request.DiceId);

                        bool isNewForUser = userDice == null;

                        if (userDice == null)
                        {
                            userDice = new DadoUsuario
                            {
                                UsuarioIdUsuario = request.UserId,
                                DadoIdDado = request.DiceId,
                                CantidadDado = 1
                            };

                            context.DadoUsuario.Add(userDice);
                        }
                        else
                        {
                            userDice.CantidadDado = userDice.CantidadDado + 1;
                            context.Entry(userDice).State = EntityState.Modified;
                        }

                        int coinsBefore = user.Monedas;
                        user.Monedas = user.Monedas - request.PriceCoins;

                        context.SaveChanges();
                        transaction.Commit();

                        ShopRewardDto reward = new ShopRewardDto
                        {
                            RewardType = ShopRewardType.Dice,
                            RewardId = dice.IdDado,
                            RewardCode = dice.CodigoDado,
                            RewardName = dice.Nombre,
                            IsNewForUser = isNewForUser,
                            CoinsBefore = coinsBefore,
                            CoinsAfter = user.Monedas
                        };

                        return OperationResult<ShopRewardDto>.Success(reward);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("SQL error while purchasing dice.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_DB);
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("EF error while purchasing dice.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Unexpected error while purchasing dice.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error creating EF context for dice purchase.", ex);
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_FATAL);
            }
        }

        public OperationResult<ShopRewardDto> PurchaseItemChest(ItemChestPurchaseDto request)
        {
            if (request == null)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }

            if (request.UserId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == request.UserId);
                        if (user == null)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_USER_NOT_FOUND);
                        }

                        if (user.Monedas < request.PriceCoins)
                        {
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INSUFFICIENT_COINS);
                        }

                        string itemCode = GetRandomItemCode();

                        Objeto item = context.Objeto
                            .SingleOrDefault(i => i.CodigoObjeto == itemCode);

                        if (item == null)
                        {
                            Logger.WarnFormat("Item not found for code {0}.", itemCode);
                            return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_ITEM_NOT_FOUND);
                        }

                        ObjetoUsuario userItem = context.ObjetoUsuario
                            .SingleOrDefault(iu =>
                                iu.UsuarioIdUsuario == request.UserId &&
                                iu.ObjetoIdObjeto == item.IdObjeto);

                        bool isNewForUser = userItem == null;

                        if (userItem == null)
                        {
                            userItem = new ObjetoUsuario
                            {
                                UsuarioIdUsuario = request.UserId,
                                ObjetoIdObjeto = item.IdObjeto,
                                CantidadObjeto = 1
                            };

                            context.ObjetoUsuario.Add(userItem);
                        }
                        else
                        {
                            userItem.CantidadObjeto = userItem.CantidadObjeto + 1;
                            context.Entry(userItem).State = EntityState.Modified;
                        }

                        int coinsBefore = user.Monedas;
                        user.Monedas = user.Monedas - request.PriceCoins;

                        context.SaveChanges();
                        transaction.Commit();

                        ShopRewardDto reward = new ShopRewardDto
                        {
                            RewardType = ShopRewardType.Item,
                            RewardId = item.IdObjeto,
                            RewardCode = item.CodigoObjeto,
                            RewardName = item.Nombre,
                            IsNewForUser = isNewForUser,
                            CoinsBefore = coinsBefore,
                            CoinsAfter = user.Monedas
                        };

                        return OperationResult<ShopRewardDto>.Success(reward);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("SQL error while purchasing item chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_DB);
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        Logger.Error("EF error while purchasing item chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Unexpected error while purchasing item chest.", ex);
                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error creating EF context for item chest.", ex);
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_FATAL);
            }
        }

        private static int GetRandomIndex(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            lock (RandomInstance)
            {
                return RandomInstance.Next(0, maxExclusive);
            }
        }

        private static string GetRandomItemCode()
        {
            int totalWeight =
                ShopRulesRepository.ITEM_WEIGHT_ROCKET +
                ShopRulesRepository.ITEM_WEIGHT_ANCHOR +
                ShopRulesRepository.ITEM_WEIGHT_SWAP +
                ShopRulesRepository.ITEM_WEIGHT_FREEZE +
                ShopRulesRepository.ITEM_WEIGHT_SHIELD;

            int roll;

            lock (RandomInstance)
            {
                roll = RandomInstance.Next(0, totalWeight);
            }

            if (roll < ShopRulesRepository.ITEM_WEIGHT_ROCKET)
            {
                return ShopRulesRepository.ITEM_CODE_ROCKET;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_ROCKET;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_ANCHOR)
            {
                return ShopRulesRepository.ITEM_CODE_ANCHOR;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_ANCHOR;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_SWAP)
            {
                return ShopRulesRepository.ITEM_CODE_SWAP;
            }

            roll -= ShopRulesRepository.ITEM_WEIGHT_SWAP;

            if (roll < ShopRulesRepository.ITEM_WEIGHT_FREEZE)
            {
                return ShopRulesRepository.ITEM_CODE_FREEZE;
            }

            return ShopRulesRepository.ITEM_CODE_SHIELD;
        }

        public OperationResult<int> GetCurrentCoins(int userId)
        {
            if (userId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                {
                    Usuario user = context.Usuario.SingleOrDefault(u => u.IdUsuario == userId);
                    if (user == null)
                    {
                        return OperationResult<int>.Failure(ShopRulesRepository.ERROR_USER_NOT_FOUND);
                    }

                    int coins = user.Monedas;
                    return OperationResult<int>.Success(coins);
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while getting current coins.", ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_DB);
            }
            catch (DbUpdateException ex)
            {
                Logger.Error("EF error while getting current coins.", ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while getting current coins.", ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }
        }

        private static int GetAvatarWeight(Avatar avatar)
        {
            if (avatar == null)
            {
                return 0;
            }

            string code = avatar.CodigoAvatar;

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_MARIA, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_MARIA;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_LIZ, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_LIZ;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_REVO, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_REVO;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_OCHARAN, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_OCHARAN;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_SAUL, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_SAUL;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_JAIME, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_JAIME;
            }

            if (string.Equals(code, ShopRulesRepository.AVATAR_CODE_WILLY, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.AVATAR_WEIGHT_WILLY;
            }

            return ShopRulesRepository.AVATAR_WEIGHT_DEFAULT;
        }

        private static Avatar GetRandomAvatar(IList<Avatar> avatars)
        {
            if (avatars == null || avatars.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(avatars));
            }

            int totalWeight = 0;

            foreach (Avatar avatar in avatars)
            {
                totalWeight += GetAvatarWeight(avatar);
            }

            if (totalWeight <= 0)
            {
                int index = GetRandomIndex(avatars.Count);
                return avatars[index];
            }

            int roll;

            lock (RandomInstance)
            {
                roll = RandomInstance.Next(0, totalWeight);
            }

            foreach (Avatar avatar in avatars)
            {
                int weight = GetAvatarWeight(avatar);

                if (weight <= 0)
                {
                    continue;
                }

                if (roll < weight)
                {
                    return avatar;
                }

                roll -= weight;
            }

            return avatars[avatars.Count - 1];
        }

        private static int GetStickerPackWeight(PaqueteStickers pack)
        {
            if (pack == null)
            {
                return 0;
            }

            string code = pack.CodigoPaqueteStickers;

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_REVO, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_REVO;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_OCHARAN, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_OCHARAN;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_LIZ, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_LIZ;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_SAUL, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_SAUL;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_JAIME, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_JAIME;
            }

            if (string.Equals(code, ShopRulesRepository.STICKER_PACK_WILLY, StringComparison.OrdinalIgnoreCase))
            {
                return ShopRulesRepository.STICKER_PACK_WEIGHT_WILLY;
            }

            return ShopRulesRepository.STICKER_PACK_WEIGHT_DEFAULT;
        }

        private static PaqueteStickers GetRandomStickerPack(IList<PaqueteStickers> packs)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(packs));
            }

            int totalWeight = 0;

            foreach (PaqueteStickers pack in packs)
            {
                totalWeight += GetStickerPackWeight(pack);
            }

            if (totalWeight <= 0)
            {
                int index = GetRandomIndex(packs.Count);
                return packs[index];
            }

            int roll;

            lock (RandomInstance)
            {
                roll = RandomInstance.Next(0, totalWeight);
            }

            foreach (PaqueteStickers pack in packs)
            {
                int weight = GetStickerPackWeight(pack);

                if (weight <= 0)
                {
                    continue;
                }

                if (roll < weight)
                {
                    return pack;
                }

                roll -= weight;
            }

            return packs[packs.Count - 1];
        }
        public OperationResult<List<StickerDto>> GetUserStickers(int userId)
        {
            if (userId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = contextFactory())
                {
                    int? defaultPackId = context.PaqueteStickers
                        .Where(p => p.CodigoPaqueteStickers == ShopRulesRepository.STICKER_PACK_DEFAULT)
                        .Select(p => (int?)p.IdPaqueteStickers)
                        .SingleOrDefault();

                    List<int> ownedPackIds = context.StickersUsuario
                        .Where(su => su.UsuarioIdUsuario == userId
                                     && su.PaqueteStickersIdPaqueteStickers.HasValue)
                        .Select(su => su.PaqueteStickersIdPaqueteStickers.Value)
                        .Distinct()
                        .ToList();

                    if (defaultPackId.HasValue && !ownedPackIds.Contains(defaultPackId.Value))
                    {
                        ownedPackIds.Add(defaultPackId.Value);
                    }

                    if (ownedPackIds.Count == 0)
                    {
                        return OperationResult<List<StickerDto>>.Success(new List<StickerDto>());
                    }

                    List<Sticker> dbStickers = context.Sticker
                        .Where(s => ownedPackIds.Contains(s.PaqueteStickersIdPaqueteStickers))
                        .ToList();

                    List<Sticker> activeStickers = dbStickers
                        .Where(s =>
                            s.Estado != null &&
                            s.Estado.Length > 0 &&
                            s.Estado[0] == STATUS_ACTIVE)
                        .ToList();

                    if (activeStickers.Count == 0)
                    {
                        return OperationResult<List<StickerDto>>.Success(new List<StickerDto>());
                    }

                    List<StickerDto> stickers = activeStickers
                        .GroupBy(s => s.CodigoSticker)
                        .Select(group => group.First())
                        .OrderBy(s => s.CodigoSticker)
                        .Select(s => new StickerDto
                        {
                            StickerId = s.IdSticker,
                            StickerCode = s.CodigoSticker,
                            StickerName = s.Nombre
                        })
                        .ToList();

                    return OperationResult<List<StickerDto>>.Success(stickers);
                }
            }
            catch (SqlException ex)
            {
                Logger.Error("SQL error while getting stickers for user.", ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_DB);
            }
            catch (DbUpdateException ex)
            {
                Logger.Error("EF error while getting stickers for user.", ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while getting stickers for user.", ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }
        }
    }

}
