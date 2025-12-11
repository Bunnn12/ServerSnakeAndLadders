using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using SnakesAndLadders.Data.Constants;
using SnakesAndLadders.Data.Helpers;

namespace ServerSnakesAndLadders
{
    public sealed class InventoryRepository : IInventoryRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(InventoryRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public InventoryRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IList<InventoryItemDto> GetUserItems(int userId)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    var query =
                        from userObject in context.ObjetoUsuario
                        join obj in context.Objeto
                            on userObject.ObjetoIdObjeto equals obj.IdObjeto
                        join selected in context.ObjetoUsuarioSeleccionado
                                .Where(s => s.UsuarioIdUsuario == userId)
                            on new
                            {
                                userObject.UsuarioIdUsuario,
                                userObject.ObjetoIdObjeto
                            }
                            equals new
                            {
                                UsuarioIdUsuario = selected.UsuarioIdUsuario,
                                ObjetoIdObjeto = selected.ObjetoIdObjeto
                            }
                            into selectedJoin
                        from selected in selectedJoin.DefaultIfEmpty()
                        where userObject.UsuarioIdUsuario == userId
                              && userObject.CantidadObjeto > 0
                        select new InventoryItemDto
                        {
                            ObjectId = obj.IdObjeto,
                            ObjectCode = obj.CodigoObjeto,
                            Name = obj.Nombre,
                            Quantity = userObject.CantidadObjeto,
                            SlotNumber = selected == null
                                ? (byte?)null
                                : selected.NumeroSlot
                        };

                    return query
                        .AsNoTracking()
                        .ToList();
                }
                catch (SqlException ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_SQL_GET_USER_ITEMS, ex);
                    return new List<InventoryItemDto>();
                }
                catch (Exception ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_UNEXPECTED_GET_USER_ITEMS, ex);
                    return new List<InventoryItemDto>();
                }
            }
        }

        public IList<InventoryDiceDto> GetUserDice(int userId)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    var query =
                        from userDice in context.DadoUsuario
                        join dice in context.Dado
                            on userDice.DadoIdDado equals dice.IdDado
                        join selected in context.DadoUsuarioSeleccionado
                                .Where(s => s.UsuarioIdUsuario == userId)
                            on new
                            {
                                userDice.UsuarioIdUsuario,
                                userDice.DadoIdDado
                            }
                            equals new
                            {
                                UsuarioIdUsuario = selected.UsuarioIdUsuario,
                                DadoIdDado = selected.DadoIdDado
                            }
                            into selectedJoin
                        from selected in selectedJoin.DefaultIfEmpty()
                        where userDice.UsuarioIdUsuario == userId
                              && userDice.CantidadDado > 0
                        select new InventoryDiceDto
                        {
                            DiceId = dice.IdDado,
                            DiceCode = dice.CodigoDado,
                            Name = dice.Nombre,
                            Quantity = userDice.CantidadDado,
                            SlotNumber = selected == null
                                ? (byte?)null
                                : selected.NumeroSlot
                        };

                    return query
                        .AsNoTracking()
                        .ToList();
                }
                catch (SqlException ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_SQL_GET_USER_DICE, ex);
                    return new List<InventoryDiceDto>();
                }
                catch (Exception ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_UNEXPECTED_GET_USER_DICE, ex);
                    return new List<InventoryDiceDto>();
                }
            }
        }

        public void UpdateSelectedItems(UpdateItemSlotsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            InventoryRepositoryHelper.EnsureValidUserId(request.UserId);

            var updateContext = new InventorySlotUpdateContext
            {
                UserId = request.UserId,
                SlotsByNumber = new Dictionary<byte, int?>
                {
                    { 1, request.Slot1ObjectId },
                    { 2, request.Slot2ObjectId },
                    { 3, request.Slot3ObjectId }
                }
            };

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                var strategy = new InventorySlotUpdateStrategy<ObjetoUsuarioSeleccionado>
                {
                    LoadSelections = (ctx, userId) => ctx.ObjetoUsuarioSeleccionado
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .ToList(),
                    GetSlotNumber = s => s.NumeroSlot,
                    IsValidSlot = InventoryRepositoryHelper.IsValidItemSlot,
                    GetItemId = s => s.ObjetoIdObjeto,
                    SetItemId = (s, id) => s.ObjetoIdObjeto = id,
                    CreateSelection = (userId, id, slot) => new ObjetoUsuarioSeleccionado
                    {
                        UsuarioIdUsuario = userId,
                        NumeroSlot = slot,
                        ObjetoIdObjeto = id
                    }
                };

                try
                {
                    InventorySlotUpdateHelper.UpdateSelections(context, updateContext, strategy);
                }
                catch (SqlException ex)
                {
                    _logger.Error("SQL error while updating selected items.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while updating selected items.", ex);
                    throw;
                }
            }
        }

        public void UpdateSelectedDice(UpdateDiceSlotsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            InventoryRepositoryHelper.EnsureValidUserId(request.UserId);

            var updateContext = new InventorySlotUpdateContext
            {
                UserId = request.UserId,
                SlotsByNumber = new Dictionary<byte, int?>
                {
                    { 1, request.Slot1DiceId },
                    { 2, request.Slot2DiceId }
                }
            };

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                var strategy = new InventorySlotUpdateStrategy<DadoUsuarioSeleccionado>
                {
                    LoadSelections = (ctx, userId) => ctx.DadoUsuarioSeleccionado
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .ToList(),
                    GetSlotNumber = s => s.NumeroSlot,
                    IsValidSlot = InventoryRepositoryHelper.IsValidDiceSlot,
                    GetItemId = s => s.DadoIdDado,
                    SetItemId = (s, id) => s.DadoIdDado = id,
                    CreateSelection = (userId, id, slot) => new DadoUsuarioSeleccionado
                    {
                        UsuarioIdUsuario = userId,
                        NumeroSlot = slot,
                        DadoIdDado = id
                    }
                };

                try
                {
                    InventorySlotUpdateHelper.UpdateSelections(context, updateContext, strategy);
                }
                catch (SqlException ex)
                {
                    _logger.Error("SQL error while updating selected dice.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while updating selected dice.", ex);
                    throw;
                }
            }
        }

        public void RemoveItemFromSlot(int userId, byte slotNumber)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (!InventoryRepositoryHelper.IsValidItemSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            var request = new UpdateItemSlotsRequest
            {
                UserId = userId
            };

            switch (slotNumber)
            {
                case 1:
                    request.Slot1ObjectId = null;
                    break;
                case 2:
                    request.Slot2ObjectId = null;
                    break;
                case 3:
                    request.Slot3ObjectId = null;
                    break;
            }

            UpdateSelectedItems(request);
        }

        public void RemoveDiceFromSlot(int userId, byte slotNumber)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (!InventoryRepositoryHelper.IsValidDiceSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            var request = new UpdateDiceSlotsRequest
            {
                UserId = userId
            };

            switch (slotNumber)
            {
                case 1:
                    request.Slot1DiceId = null;
                    break;
                case 2:
                    request.Slot2DiceId = null;
                    break;
            }

            UpdateSelectedDice(request);
        }

        public void ConsumeItem(int userId, int objectId)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (objectId < InventoryRepositoryConstants.MIN_OBJECT_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(objectId),
                    InventoryRepositoryConstants.ERROR_OBJECT_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    ObjetoUsuario userObject = context.ObjetoUsuario
                        .SingleOrDefault(
                            o => o.UsuarioIdUsuario == userId
                                 && o.ObjetoIdObjeto == objectId);

                    if (userObject == null)
                    {
                        throw new InvalidOperationException(
                            InventoryRepositoryConstants.ERROR_USER_DOES_NOT_OWN_ITEM);
                    }

                    if (userObject.CantidadObjeto <= 0)
                    {
                        throw new InvalidOperationException(
                            InventoryRepositoryConstants.ERROR_USER_HAS_NO_ITEM_QUANTITY);
                    }

                    userObject.CantidadObjeto -= 1;

                    if (userObject.CantidadObjeto <= 0)
                    {
                        userObject.CantidadObjeto = 0;

                        List<ObjetoUsuarioSeleccionado> selectedEntries =
                            context.ObjetoUsuarioSeleccionado
                                .Where(s => s.UsuarioIdUsuario == userId
                                            && s.ObjetoIdObjeto == objectId)
                                .ToList();

                        foreach (ObjetoUsuarioSeleccionado entry in selectedEntries)
                        {
                            context.ObjetoUsuarioSeleccionado.Remove(entry);
                        }
                    }

                    context.Entry(userObject).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    _logger.Error("SQL error while consuming item.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while consuming item.", ex);
                    throw;
                }
            }
        }

        public void ConsumeDice(int userId, int diceId)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (diceId < InventoryRepositoryConstants.MIN_DICE_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(diceId),
                    InventoryRepositoryConstants.ERROR_DICE_ID_POSITIVE);
            }

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    DadoUsuario userDice = context.DadoUsuario
                        .SingleOrDefault(
                            d => d.UsuarioIdUsuario == userId
                                 && d.DadoIdDado == diceId);

                    if (userDice == null)
                    {
                        throw new InvalidOperationException(
                            InventoryRepositoryConstants.ERROR_USER_DOES_NOT_OWN_DICE);
                    }

                    if (userDice.CantidadDado <= 0)
                    {
                        throw new InvalidOperationException(
                            InventoryRepositoryConstants.ERROR_USER_HAS_NO_DICE_QUANTITY);
                    }

                    userDice.CantidadDado -= 1;

                    if (userDice.CantidadDado < 0)
                    {
                        userDice.CantidadDado = 0;
                    }

                    List<DadoUsuarioSeleccionado> selectedEntries =
                        context.DadoUsuarioSeleccionado
                            .Where(s => s.UsuarioIdUsuario == userId
                                        && s.DadoIdDado == diceId)
                            .ToList();

                    foreach (DadoUsuarioSeleccionado entry in selectedEntries)
                    {
                        context.DadoUsuarioSeleccionado.Remove(entry);
                    }

                    context.Entry(userDice).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    _logger.Error("SQL error while consuming dice.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while consuming dice.", ex);
                    throw;
                }
            }
        }

        public OperationResult<bool> GrantItemToUser(int userId, string itemCode)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentException(
                    InventoryRepositoryConstants.ERROR_ITEM_CODE_REQUIRED,
                    nameof(itemCode));
            }

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    Objeto item = context.Objeto
                        .SingleOrDefault(o => o.CodigoObjeto == itemCode);

                    if (item == null)
                    {
                        string message = string.Format(
                            InventoryRepositoryConstants.ERROR_ITEM_CONFIG_NOT_FOUND_TEMPLATE,
                            itemCode);

                        return OperationResult<bool>.Failure(message);
                    }

                    ObjetoUsuario userItem = context.ObjetoUsuario
                        .SingleOrDefault(ou =>
                            ou.UsuarioIdUsuario == userId &&
                            ou.ObjetoIdObjeto == item.IdObjeto);

                    if (userItem == null)
                    {
                        userItem = new ObjetoUsuario
                        {
                            UsuarioIdUsuario = userId,
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

                    InventoryRepositoryHelper.TryEquipItemIfSlotAvailable(context, userId, item.IdObjeto);

                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
                catch (SqlException ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_SQL_GRANT_ITEM, ex);
                    return OperationResult<bool>.Failure(
                        InventoryRepositoryConstants.ERROR_DATABASE_GRANTING_ITEM);
                }
                catch (Exception ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_UNEXPECTED_GRANT_ITEM, ex);
                    return OperationResult<bool>.Failure(
                        InventoryRepositoryConstants.ERROR_UNEXPECTED_GRANTING_ITEM);
                }
            }
        }

        public OperationResult<bool> GrantDiceToUser(int userId, string diceCode)
        {
            InventoryRepositoryHelper.EnsureValidUserId(userId);

            if (string.IsNullOrWhiteSpace(diceCode))
            {
                throw new ArgumentException(
                    InventoryRepositoryConstants.ERROR_DICE_CODE_REQUIRED,
                    nameof(diceCode));
            }

            using (SnakeAndLaddersDBEntities1 context = _contextFactory())
            {
                InventoryRepositoryHelper.ConfigureContext(context);

                try
                {
                    Dado dice = context.Dado
                        .SingleOrDefault(d => d.CodigoDado == diceCode);

                    if (dice == null)
                    {
                        string message = string.Format(
                            InventoryRepositoryConstants.ERROR_DICE_CONFIG_NOT_FOUND_TEMPLATE,
                            diceCode);

                        return OperationResult<bool>.Failure(message);
                    }

                    DadoUsuario userDice = context.DadoUsuario
                        .SingleOrDefault(du =>
                            du.UsuarioIdUsuario == userId &&
                            du.DadoIdDado == dice.IdDado);

                    if (userDice == null)
                    {
                        userDice = new DadoUsuario
                        {
                            UsuarioIdUsuario = userId,
                            DadoIdDado = dice.IdDado,
                            CantidadDado = 1
                        };

                        context.DadoUsuario.Add(userDice);
                    }
                    else
                    {
                        userDice.CantidadDado += 1;
                        context.Entry(userDice).State = EntityState.Modified;
                    }

                    InventoryRepositoryHelper.TryEquipDiceIfSlotAvailable(context, userId, dice.IdDado);

                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
                catch (SqlException ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_SQL_GRANT_DICE, ex);
                    return OperationResult<bool>.Failure(
                        InventoryRepositoryConstants.ERROR_DATABASE_GRANTING_DICE);
                }
                catch (Exception ex)
                {
                    _logger.Error(InventoryRepositoryConstants.LOG_UNEXPECTED_GRANT_DICE, ex);
                    return OperationResult<bool>.Failure(
                        InventoryRepositoryConstants.ERROR_UNEXPECTED_GRANTING_DICE);
                }
            }
        }
    }
}
