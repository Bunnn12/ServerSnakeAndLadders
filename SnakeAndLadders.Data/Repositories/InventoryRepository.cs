using log4net;
using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Interfaces;
using SnakesAndLadders.Data;
using System;
using System.Collections.Generic;
using ServerSnakesAndLadders.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace ServerSnakesAndLadders
{
    public class InventoryRepository : IInventoryRepository
    {
        private const int CommandTimeoutSeconds = 30;
        private const int MinValidUserId = 1;
        private const byte MinSlotNumber = 1;
        private const byte MaxItemSlots = 3;
        private const byte MaxDiceSlots = 2;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryRepository));

        private readonly Func<SnakeAndLaddersDBEntities1> _contextFactory;

        public InventoryRepository(Func<SnakeAndLaddersDBEntities1> contextFactory = null)
        {
            _contextFactory = contextFactory ?? (() => new SnakeAndLaddersDBEntities1());
        }

        public IList<InventoryItemDto> GetUserItems(int userId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

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
                    Logger.Error("Error SQL al obtener el inventario de objetos del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al obtener el inventario de objetos del usuario.", ex);
                    throw;
                }
            }
        }


        public IList<InventoryDiceDto> GetUserDice(int userId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

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
                    Logger.Error("Error SQL al obtener el inventario de dados del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al obtener el inventario de dados del usuario.", ex);
                    throw;
                }
            }
        }


        public void UpdateSelectedItems(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            var slots = new Dictionary<byte, int?>
            {
                { 1, slot1ObjectId },
                { 2, slot2ObjectId },
                { 3, slot3ObjectId }
            };

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var existingSelections = context.ObjetoUsuarioSeleccionado
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (var slotEntry in slots)
                    {
                        byte slotNumber = slotEntry.Key;
                        int? objectId = slotEntry.Value;

                        if (!IsValidItemSlot(slotNumber))
                        {
                            continue;
                        }

                        var entity = existingSelections
                            .SingleOrDefault(s => s.NumeroSlot == slotNumber);

                        if (objectId.HasValue)
                        {
                            if (entity == null)
                            {
                                entity = new ObjetoUsuarioSeleccionado
                                {
                                    UsuarioIdUsuario = userId,
                                    NumeroSlot = slotNumber,
                                    ObjetoIdObjeto = objectId.Value
                                };

                                context.ObjetoUsuarioSeleccionado.Add(entity);
                            }
                            else
                            {
                                entity.ObjetoIdObjeto = objectId.Value;
                                context.Entry(entity).State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            if (entity != null)
                            {
                                context.ObjetoUsuarioSeleccionado.Remove(entity);
                            }
                        }
                    }

                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al actualizar los objetos seleccionados del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al actualizar los objetos seleccionados del usuario.", ex);
                    throw;
                }
            }
        }

        public void UpdateSelectedDice(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            var slots = new Dictionary<byte, int?>
            {
                { 1, slot1DiceId },
                { 2, slot2DiceId }
            };

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var existingSelections = context.DadoUsuarioSeleccionado
                        .Where(s => s.UsuarioIdUsuario == userId)
                        .ToList();

                    foreach (var slotEntry in slots)
                    {
                        byte slotNumber = slotEntry.Key;
                        int? diceId = slotEntry.Value;

                        if (!IsValidDiceSlot(slotNumber))
                        {
                            continue;
                        }

                        var entity = existingSelections
                            .SingleOrDefault(s => s.NumeroSlot == slotNumber);

                        if (diceId.HasValue)
                        {
                            if (entity == null)
                            {
                                entity = new DadoUsuarioSeleccionado
                                {
                                    UsuarioIdUsuario = userId,
                                    NumeroSlot = slotNumber,
                                    DadoIdDado = diceId.Value
                                };

                                context.DadoUsuarioSeleccionado.Add(entity);
                            }
                            else
                            {
                                entity.DadoIdDado = diceId.Value;
                                context.Entry(entity).State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            if (entity != null)
                            {
                                context.DadoUsuarioSeleccionado.Remove(entity);
                            }
                        }
                    }

                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al actualizar los dados seleccionados del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al actualizar los dados seleccionados del usuario.", ex);
                    throw;
                }
            }
        }

        public void RemoveItemFromSlot(int userId, byte slotNumber)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidItemSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var entity = context.ObjetoUsuarioSeleccionado
                        .SingleOrDefault(
                            s => s.UsuarioIdUsuario == userId
                                 && s.NumeroSlot == slotNumber);

                    if (entity != null)
                    {
                        context.ObjetoUsuarioSeleccionado.Remove(entity);
                        context.SaveChanges();
                    }
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al quitar el objeto seleccionado del slot.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al quitar el objeto seleccionado del slot.", ex);
                    throw;
                }
            }
        }

        public void RemoveDiceFromSlot(int userId, byte slotNumber)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (!IsValidDiceSlot(slotNumber))
            {
                throw new ArgumentOutOfRangeException(nameof(slotNumber));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var entity = context.DadoUsuarioSeleccionado
                        .SingleOrDefault(
                            s => s.UsuarioIdUsuario == userId
                                 && s.NumeroSlot == slotNumber);

                    if (entity != null)
                    {
                        context.DadoUsuarioSeleccionado.Remove(entity);
                        context.SaveChanges();
                    }
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al quitar el dado seleccionado del slot.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al quitar el dado seleccionado del slot.", ex);
                    throw;
                }
            }
        }

        public void ConsumeItem(int userId, int objectId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (objectId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(objectId));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var userObject = context.ObjetoUsuario
                        .SingleOrDefault(
                            o => o.UsuarioIdUsuario == userId
                                 && o.ObjetoIdObjeto == objectId);

                    if (userObject == null)
                    {
                        throw new InvalidOperationException(
                            "El usuario no posee el objeto especificado en su inventario.");
                    }

                    if (userObject.CantidadObjeto <= 0)
                    {
                        throw new InvalidOperationException(
                            "El usuario no tiene cantidad disponible del objeto especificado.");
                    }

                    userObject.CantidadObjeto -= 1;

                    
                    if (userObject.CantidadObjeto <= 0)
                    {
                        userObject.CantidadObjeto = 0;

                        var selectedEntries = context.ObjetoUsuarioSeleccionado
                            .Where(s => s.UsuarioIdUsuario == userId
                                        && s.ObjetoIdObjeto == objectId)
                            .ToList();

                        foreach (var entry in selectedEntries)
                        {
                            context.ObjetoUsuarioSeleccionado.Remove(entry);
                        }
                    }

                    context.Entry(userObject).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al consumir un objeto del inventario del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al consumir un objeto del inventario del usuario.", ex);
                    throw;
                }
            }
        }


        public void ConsumeDice(int userId, int diceId)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (diceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(diceId));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var userDice = context.DadoUsuario
                        .SingleOrDefault(
                            d => d.UsuarioIdUsuario == userId
                                 && d.DadoIdDado == diceId);

                    if (userDice == null)
                    {
                        throw new InvalidOperationException(
                            "El usuario no posee el dado especificado en su inventario.");
                    }

                    if (userDice.CantidadDado <= 0)
                    {
                        throw new InvalidOperationException(
                            "El usuario no tiene cantidad disponible del dado especificado.");
                    }

                
                    userDice.CantidadDado -= 1;

                    if (userDice.CantidadDado < 0)
                    {
                        userDice.CantidadDado = 0;
                    }

                    var selectedEntries = context.DadoUsuarioSeleccionado
                        .Where(s => s.UsuarioIdUsuario == userId
                                    && s.DadoIdDado == diceId)
                        .ToList();

                    foreach (var entry in selectedEntries)
                    {
                        context.DadoUsuarioSeleccionado.Remove(entry);
                    }

                    context.Entry(userDice).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al consumir un dado del inventario del usuario.", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al consumir un dado del inventario del usuario.", ex);
                    throw;
                }
            }
        }
        private void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = CommandTimeoutSeconds;
        }

        private static bool IsValidUserId(int userId)
        {
            return userId >= MinValidUserId;
        }

        private static bool IsValidItemSlot(byte slotNumber)
        {
            return slotNumber >= MinSlotNumber && slotNumber <= MaxItemSlots;
        }

        private static bool IsValidDiceSlot(byte slotNumber)
        {
            return slotNumber >= MinSlotNumber && slotNumber <= MaxDiceSlots;
        }

        public OperationResult<bool> GrantItemToUser(int userId, string itemCode)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentException("itemCode es obligatorio.", nameof(itemCode));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var item = context.Objeto
                        .SingleOrDefault(o => o.CodigoObjeto == itemCode);

                    if (item == null)
                    {
                        return OperationResult<bool>.Failure(
                            $"No existe un objeto configurado con el código '{itemCode}'.");
                    }

                    var userItem = context.ObjetoUsuario
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

                    // auto-equipar si hay slot libre
                    TryEquipItemIfSlotAvailable(context, userId, item.IdObjeto);

                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al otorgar un objeto al usuario.", ex);
                    return OperationResult<bool>.Failure("Database error while granting item to user.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al otorgar un objeto al usuario.", ex);
                    return OperationResult<bool>.Failure("Unexpected error while granting item to user.");
                }
            }
        }

        public OperationResult<bool> GrantDiceToUser(int userId, string diceCode)
        {
            if (!IsValidUserId(userId))
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(diceCode))
            {
                throw new ArgumentException("diceCode es obligatorio.", nameof(diceCode));
            }

            using (var context = _contextFactory())
            {
                ConfigureContext(context);

                try
                {
                    var dice = context.Dado
                        .SingleOrDefault(d => d.CodigoDado == diceCode);

                    if (dice == null)
                    {
                        return OperationResult<bool>.Failure(
                            $"No existe un dado configurado con el código '{diceCode}'.");
                    }

                    var userDice = context.DadoUsuario
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

                    // auto-equipar si hay slot libre
                    TryEquipDiceIfSlotAvailable(context, userId, dice.IdDado);

                    context.SaveChanges();

                    return OperationResult<bool>.Success(true);
                }
                catch (SqlException ex)
                {
                    Logger.Error("Error SQL al otorgar un dado al usuario.", ex);
                    return OperationResult<bool>.Failure("Database error while granting dice to user.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error inesperado al otorgar un dado al usuario.", ex);
                    return OperationResult<bool>.Failure("Unexpected error while granting dice to user.");
                }
            }
        }


        private void TryEquipItemIfSlotAvailable(
    SnakeAndLaddersDBEntities1 context,
    int userId,
    int objectId)
        {
            // 1) Si este tipo de objeto YA está equipado en algún slot, no hacer nada.
            bool alreadyEquipped = context.ObjetoUsuarioSeleccionado
                .Any(s =>
                    s.UsuarioIdUsuario == userId &&
                    s.ObjetoIdObjeto == objectId);

            if (alreadyEquipped)
            {
                // Ya tiene un escudo (o el item que sea) en algún slot.
                // Solo aumentamos la cantidad en ObjetoUsuario, pero no tocamos slots.
                return;
            }

            // 2) Buscar un slot libre
            var usedSlots = context.ObjetoUsuarioSeleccionado
                .Where(s => s.UsuarioIdUsuario == userId)
                .Select(s => s.NumeroSlot)
                .ToList();

            for (byte slot = MinSlotNumber; slot <= MaxItemSlots; slot++)
            {
                if (!usedSlots.Contains(slot))
                {
                    var selected = new ObjetoUsuarioSeleccionado
                    {
                        UsuarioIdUsuario = userId,
                        NumeroSlot = slot,
                        ObjetoIdObjeto = objectId
                    };

                    context.ObjetoUsuarioSeleccionado.Add(selected);
                    break;
                }
            }
        }


        private void TryEquipDiceIfSlotAvailable(
    SnakeAndLaddersDBEntities1 context,
    int userId,
    int diceId)
        {
            // 1) Si este tipo de dado YA está equipado en algún slot, no hacer nada.
            bool alreadyEquipped = context.DadoUsuarioSeleccionado
                .Any(s =>
                    s.UsuarioIdUsuario == userId &&
                    s.DadoIdDado == diceId);

            if (alreadyEquipped)
            {
                // Ya tiene ese dado equipado en algún slot.
                // Solo aumentamos la cantidad en DadoUsuario, no tocamos slots.
                return;
            }

            // 2) Buscar un slot libre
            var usedSlots = context.DadoUsuarioSeleccionado
                .Where(s => s.UsuarioIdUsuario == userId)
                .Select(s => s.NumeroSlot)
                .ToList();

            for (byte slot = MinSlotNumber; slot <= MaxDiceSlots; slot++)
            {
                if (!usedSlots.Contains(slot))
                {
                    var selected = new DadoUsuarioSeleccionado
                    {
                        UsuarioIdUsuario = userId,
                        NumeroSlot = slot,
                        DadoIdDado = diceId
                    };

                    context.DadoUsuarioSeleccionado.Add(selected);
                    break;
                }
            }
        }







    }
}
