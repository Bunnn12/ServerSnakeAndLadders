using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SnakesAndLadders.Data.Constants;

namespace SnakesAndLadders.Data.Helpers
{
    internal static class InventoryRepositoryHelper
    {
        public static void ConfigureContext(SnakeAndLaddersDBEntities1 context)
        {
            ((IObjectContextAdapter)context).ObjectContext.CommandTimeout =
                InventoryRepositoryConstants.COMMAND_TIMEOUT_SECONDS;
        }

        public static void EnsureValidUserId(int userId)
        {
            if (userId < InventoryRepositoryConstants.MIN_VALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(userId),
                    InventoryRepositoryConstants.ERROR_USER_ID_POSITIVE);
            }
        }

        public static bool IsValidItemSlot(byte slotNumber)
        {
            return slotNumber >= InventoryRepositoryConstants.MIN_SLOT_NUMBER
                   && slotNumber <= InventoryRepositoryConstants.MAX_ITEM_SLOTS;
        }

        public static bool IsValidDiceSlot(byte slotNumber)
        {
            return slotNumber >= InventoryRepositoryConstants.MIN_SLOT_NUMBER
                   && slotNumber <= InventoryRepositoryConstants.MAX_DICE_SLOTS;
        }

        public static void TryEquipItemIfSlotAvailable(
            SnakeAndLaddersDBEntities1 context,
            int userId,
            int objectId)
        {
            bool alreadyEquipped = context.ObjetoUsuarioSeleccionado
                .Any(s =>
                    s.UsuarioIdUsuario == userId &&
                    s.ObjetoIdObjeto == objectId);

            if (alreadyEquipped)
            {
                return;
            }

            List<byte> usedSlots = context.ObjetoUsuarioSeleccionado
                .Where(s => s.UsuarioIdUsuario == userId)
                .Select(s => s.NumeroSlot)
                .ToList();

            for (byte slot = InventoryRepositoryConstants.MIN_SLOT_NUMBER;
                 slot <= InventoryRepositoryConstants.MAX_ITEM_SLOTS;
                 slot++)
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

        public static void TryEquipDiceIfSlotAvailable(
            SnakeAndLaddersDBEntities1 context,
            int userId,
            int diceId)
        {
            bool alreadyEquipped = context.DadoUsuarioSeleccionado
                .Any(s =>
                    s.UsuarioIdUsuario == userId &&
                    s.DadoIdDado == diceId);

            if (alreadyEquipped)
            {
                return;
            }

            List<byte> usedSlots = context.DadoUsuarioSeleccionado
                .Where(s => s.UsuarioIdUsuario == userId)
                .Select(s => s.NumeroSlot)
                .ToList();

            for (byte slot = InventoryRepositoryConstants.MIN_SLOT_NUMBER;
                 slot <= InventoryRepositoryConstants.MAX_DICE_SLOTS;
                 slot++)
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
