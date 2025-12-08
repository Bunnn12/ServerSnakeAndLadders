using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    internal sealed class GameplayInventoryService
    {
        private readonly IInventoryRepository inventoryRepository;
        private readonly ILog logger;

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, PendingRocketUsage>> pendingRocketsByGameId =
            new ConcurrentDictionary<int, ConcurrentDictionary<int, PendingRocketUsage>>();

        public GameplayInventoryService(
            IInventoryRepository inventoryRepository,
            ILog logger)
        {
            this.inventoryRepository = inventoryRepository
                ?? throw new ArgumentNullException(nameof(inventoryRepository));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public InventoryItemDto ResolveEquippedItemForSlot(
            int userId,
            byte slotNumber,
            string noItemMessage,
            string noQuantityMessage,
            string dbErrorMessage)
        {
            try
            {
                var items = inventoryRepository.GetUserItems(userId);

                InventoryItemDto equippedItem = items
                    .FirstOrDefault(i => i.SlotNumber.HasValue && i.SlotNumber.Value == slotNumber);

                if (equippedItem == null)
                {
                    throw new FaultException(noItemMessage);
                }

                if (equippedItem.Quantity <= 0)
                {
                    throw new FaultException(noQuantityMessage);
                }

                return equippedItem;
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("Error while resolving equipped item for slot.", ex);
                throw new FaultException(dbErrorMessage);
            }
        }

        public InventoryDiceDto ResolveEquippedDiceForSlot(
            int userId,
            byte slotNumber,
            string noDiceMessage,
            string noQuantityMessage,
            string dbErrorMessage)
        {
            try
            {
                var diceList = inventoryRepository.GetUserDice(userId);

                InventoryDiceDto equippedDice = diceList
                    .FirstOrDefault(d => d.SlotNumber.HasValue && d.SlotNumber.Value == slotNumber);

                if (equippedDice == null)
                {
                    throw new FaultException(noDiceMessage);
                }

                if (equippedDice.Quantity <= 0)
                {
                    throw new FaultException(noQuantityMessage);
                }

                return equippedDice;
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("Error while resolving equipped dice for slot.", ex);
                throw new FaultException(dbErrorMessage);
            }
        }

        public void GrantRewardsFromSpecialCells(
            int userId,
            RollDiceResult moveResult,
            string logPrefix)
        {
            if (moveResult == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(moveResult.GrantedItemCode))
                {
                    OperationResult<bool> itemResult = inventoryRepository.GrantItemToUser(
                        userId,
                        moveResult.GrantedItemCode);

                    if (!itemResult.IsSuccess)
                    {
                        logger.WarnFormat(
                            "{0} ItemCode={1}, UserId={2}, Reason={3}",
                            logPrefix,
                            moveResult.GrantedItemCode,
                            userId,
                            itemResult.ErrorMessage);
                    }
                }

                if (!string.IsNullOrWhiteSpace(moveResult.GrantedDiceCode))
                {
                    OperationResult<bool> diceResult = inventoryRepository.GrantDiceToUser(
                        userId,
                        moveResult.GrantedDiceCode);

                    if (!diceResult.IsSuccess)
                    {
                        logger.WarnFormat(
                            "{0} DiceCode={1}, UserId={2}, Reason={3}",
                            logPrefix,
                            moveResult.GrantedDiceCode,
                            userId,
                            diceResult.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(logPrefix, ex);
            }
        }

        public void TrackPendingRocket(
            int gameId,
            int userId,
            byte slotNumber,
            int objectId,
            string itemCode)
        {
            var pendingForGame = pendingRocketsByGameId.GetOrAdd(
                gameId,
                _ => new ConcurrentDictionary<int, PendingRocketUsage>());

            pendingForGame[userId] = new PendingRocketUsage
            {
                GameId = gameId,
                UserId = userId,
                SlotNumber = slotNumber,
                ObjectId = objectId,
                ItemCode = itemCode
            };
        }

        public void HandlePendingRocketConsumption(
            int gameId,
            int userId,
            RollDiceResult moveResult,
            string rocketUsedToken,
            string rocketIgnoredToken)
        {
            if (moveResult == null)
            {
                return;
            }

            if (!pendingRocketsByGameId.TryGetValue(gameId, out var pendingForGame))
            {
                return;
            }

            if (!pendingForGame.TryGetValue(userId, out PendingRocketUsage pending))
            {
                return;
            }

            string extraInfo = moveResult.ExtraInfo ?? string.Empty;
            string normalized = extraInfo.ToUpperInvariant();

            bool rocketUsed = normalized.Contains(rocketUsedToken);
            bool rocketIgnored = normalized.Contains(rocketIgnoredToken);

            if (rocketUsed)
            {
                try
                {
                    inventoryRepository.ConsumeItem(userId, pending.ObjectId);
                    inventoryRepository.RemoveItemFromSlot(userId, pending.SlotNumber);
                }
                catch (Exception ex)
                {
                    logger.Error("Error while consuming rocket item after dice roll.", ex);
                }
            }

            pendingForGame.TryRemove(userId, out _);
        }
    }
}
