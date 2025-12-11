using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Enums;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace SnakesAndLadders.Data.Repositories
{
    public sealed class ShopRepository : IShopRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ShopRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        private sealed class TransactionLogMessages
        {
            public string SqlErrorLogMessage { get; set; }

            public string EfErrorLogMessage { get; set; }

            public string UnexpectedErrorLogMessage { get; set; }

            public string FatalErrorLogMessage { get; set; }
        }

        public ShopRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
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

            TransactionLogMessages logMessages = new TransactionLogMessages
            {
                SqlErrorLogMessage = ShopRepositoryConstants.LOG_SQL_ERROR_PURCHASE_AVATAR_CHEST,
                EfErrorLogMessage = ShopRepositoryConstants.LOG_EF_ERROR_PURCHASE_AVATAR_CHEST,
                UnexpectedErrorLogMessage = ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_PURCHASE_AVATAR_CHEST,
                FatalErrorLogMessage = ShopRepositoryConstants.LOG_FATAL_ERROR_CREATE_CONTEXT_AVATAR_CHEST
            };

            return ExecuteInTransaction(
                logMessages,
                context =>
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
                        _logger.WarnFormat(
                            ShopRepositoryConstants.LOG_WARN_NO_AVATAR_CANDIDATES_FOR_RARITY,
                            rarityText);

                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_NO_AVATARS_FOR_RARITY);
                    }

                    List<Avatar> notOwned = candidates
                        .Where(a => !unlockedIds.Contains(a.IdAvatar))
                        .ToList();

                    List<Avatar> selectionPool = notOwned.Any()
                        ? notOwned
                        : candidates;

                    Avatar selectedAvatar = ShopSelectionHelper.GetRandomAvatar(selectionPool);

                    bool isNewForUser = !unlockedIds.Contains(selectedAvatar.IdAvatar);

                    int coinsBefore = user.Monedas;
                    user.Monedas -= request.PriceCoins;

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
                });
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

            TransactionLogMessages logMessages = new TransactionLogMessages
            {
                SqlErrorLogMessage = ShopRepositoryConstants.LOG_SQL_ERROR_PURCHASE_STICKER_CHEST,
                EfErrorLogMessage = ShopRepositoryConstants.LOG_EF_ERROR_PURCHASE_STICKER_CHEST,
                UnexpectedErrorLogMessage = ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_PURCHASE_STICKER_CHEST,
                FatalErrorLogMessage = ShopRepositoryConstants.LOG_FATAL_ERROR_CREATE_CONTEXT_STICKER_CHEST
            };

            return ExecuteInTransaction(
                logMessages,
                context =>
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
                        .Where(p => ShopSelectionHelper.GetStickerPackRarity(p.CodigoPaqueteStickers) == request.Rarity)
                        .ToList();

                    if (!candidates.Any())
                    {
                        _logger.WarnFormat(
                            ShopRepositoryConstants.LOG_WARN_NO_STICKER_PACK_CANDIDATES_FOR_RARITY,
                            request.Rarity);

                        return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_NO_STICKER_PACKS);
                    }

                    List<PaqueteStickers> notOwned = candidates
                        .Where(p => !ownedPackIds.Contains(p.IdPaqueteStickers))
                        .ToList();

                    List<PaqueteStickers> selectionPool = notOwned.Any()
                        ? notOwned
                        : candidates;

                    PaqueteStickers selectedPack = ShopSelectionHelper.GetRandomStickerPack(selectionPool);

                    bool isNewForUser = !ownedPackIds.Contains(selectedPack.IdPaqueteStickers);

                    int coinsBefore = user.Monedas;
                    user.Monedas -= request.PriceCoins;

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
                });
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

            if (request.DiceId < ShopRepositoryConstants.MIN_VALID_DICE_ID)
            {
                return OperationResult<ShopRewardDto>.Failure(ShopRulesRepository.ERROR_INVALID_DICE_ID);
            }

            TransactionLogMessages logMessages = new TransactionLogMessages
            {
                SqlErrorLogMessage = ShopRepositoryConstants.LOG_SQL_ERROR_PURCHASE_DICE,
                EfErrorLogMessage = ShopRepositoryConstants.LOG_EF_ERROR_PURCHASE_DICE,
                UnexpectedErrorLogMessage = ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_PURCHASE_DICE,
                FatalErrorLogMessage = ShopRepositoryConstants.LOG_FATAL_ERROR_CREATE_CONTEXT_DICE
            };

            return ExecuteInTransaction(
                logMessages,
                context =>
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
                        userDice.CantidadDado += 1;
                        context.Entry(userDice).State = EntityState.Modified;
                    }

                    int coinsBefore = user.Monedas;
                    user.Monedas -= request.PriceCoins;

                    context.SaveChanges();

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
                });
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

            TransactionLogMessages logMessages = new TransactionLogMessages
            {
                SqlErrorLogMessage = ShopRepositoryConstants.LOG_SQL_ERROR_PURCHASE_ITEM_CHEST,
                EfErrorLogMessage = ShopRepositoryConstants.LOG_EF_ERROR_PURCHASE_ITEM_CHEST,
                UnexpectedErrorLogMessage = ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_PURCHASE_ITEM_CHEST,
                FatalErrorLogMessage = ShopRepositoryConstants.LOG_FATAL_ERROR_CREATE_CONTEXT_ITEM_CHEST
            };

            return ExecuteInTransaction(
                logMessages,
                context =>
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

                    string itemCode = ShopSelectionHelper.GetRandomItemCode();

                    Objeto item = context.Objeto
                        .SingleOrDefault(i => i.CodigoObjeto == itemCode);

                    if (item == null)
                    {
                        _logger.WarnFormat(
                            ShopRepositoryConstants.LOG_WARN_ITEM_NOT_FOUND_FOR_CODE,
                            itemCode);

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
                        userItem.CantidadObjeto += 1;
                        context.Entry(userItem).State = EntityState.Modified;
                    }

                    int coinsBefore = user.Monedas;
                    user.Monedas -= request.PriceCoins;

                    context.SaveChanges();

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
                });
        }

        public OperationResult<int> GetCurrentCoins(int userId)
        {
            if (userId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
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
                _logger.Error(ShopRepositoryConstants.LOG_SQL_ERROR_GET_CURRENT_COINS, ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_DB);
            }
            catch (DbUpdateException ex)
            {
                _logger.Error(ShopRepositoryConstants.LOG_EF_ERROR_GET_CURRENT_COINS, ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
            }
            catch (Exception ex)
            {
                _logger.Error(ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_GET_CURRENT_COINS, ex);
                return OperationResult<int>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }
        }

        public OperationResult<List<StickerDto>> GetUserStickers(int userId)
        {
            if (userId < ShopRulesRepository.MIN_USER_ID)
            {
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_INVALID_USER_ID);
            }

            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
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
                        .Where(ShopStickerHelper.IsActiveSticker)
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
                _logger.Error(ShopRepositoryConstants.LOG_SQL_ERROR_GET_STICKERS_FOR_USER, ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_DB);
            }
            catch (DbUpdateException ex)
            {
                _logger.Error(ShopRepositoryConstants.LOG_EF_ERROR_GET_STICKERS_FOR_USER, ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
            }
            catch (Exception ex)
            {
                _logger.Error(ShopRepositoryConstants.LOG_UNEXPECTED_ERROR_GET_STICKERS_FOR_USER, ex);
                return OperationResult<List<StickerDto>>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
            }
        }

        private OperationResult<T> ExecuteInTransaction<T>(
            TransactionLogMessages logMessages,
            Func<SnakeAndLaddersDBEntities1, OperationResult<T>> operation)
        {
            try
            {
                using (SnakeAndLaddersDBEntities1 context = _contextFactory())
                using (DbContextTransaction transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        OperationResult<T> result = operation(context);

                        transaction.Commit();
                        return result;
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        _logger.Error(logMessages.SqlErrorLogMessage, ex);
                        return OperationResult<T>.Failure(ShopRulesRepository.ERROR_DB);
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        _logger.Error(logMessages.EfErrorLogMessage, ex);
                        return OperationResult<T>.Failure(ShopRulesRepository.ERROR_PERSISTENCE);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.Error(logMessages.UnexpectedErrorLogMessage, ex);
                        return OperationResult<T>.Failure(ShopRulesRepository.ERROR_UNEXPECTED);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(logMessages.FatalErrorLogMessage, ex);
                return OperationResult<T>.Failure(ShopRulesRepository.ERROR_FATAL);
            }
        }
    }
}
