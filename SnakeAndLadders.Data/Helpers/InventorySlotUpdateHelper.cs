using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace SnakesAndLadders.Data.Helpers
{
    internal sealed class InventorySlotUpdateContext
    {
        public int UserId { get; set; }

        public IDictionary<byte, int?> SlotsByNumber { get; set; }
    }

    internal sealed class InventorySlotUpdateStrategy<TSelection>
        where TSelection : class
    {
        public Func<SnakeAndLaddersDBEntities1, int, IList<TSelection>> LoadSelections { get; set; }

        public Func<TSelection, byte> GetSlotNumber { get; set; }

        public Func<byte, bool> IsValidSlot { get; set; }

        public Func<TSelection, int> GetItemId { get; set; }

        public Action<TSelection, int> SetItemId { get; set; }

        public Func<int, int, byte, TSelection> CreateSelection { get; set; }
    }

    internal static class InventorySlotUpdateHelper
    {
        public static void UpdateSelections<TSelection>(
            SnakeAndLaddersDBEntities1 context,
            InventorySlotUpdateContext updateContext,
            InventorySlotUpdateStrategy<TSelection> strategy)
            where TSelection : class
        {
            IList<TSelection> existingSelections =
                strategy.LoadSelections(context, updateContext.UserId);

            foreach (KeyValuePair<byte, int?> slotEntry in updateContext.SlotsByNumber)
            {
                byte slotNumber = slotEntry.Key;
                int? itemId = slotEntry.Value;

                if (!strategy.IsValidSlot(slotNumber))
                {
                    continue;
                }

                TSelection entity = existingSelections
                    .SingleOrDefault(s => strategy.GetSlotNumber(s) == slotNumber);

                if (itemId.HasValue)
                {
                    if (entity == null)
                    {
                        entity = strategy.CreateSelection(
                            updateContext.UserId,
                            itemId.Value,
                            slotNumber);

                        context.Set<TSelection>().Add(entity);
                    }
                    else
                    {
                        strategy.SetItemId(entity, itemId.Value);
                        context.Entry(entity).State = EntityState.Modified;
                    }
                }
                else
                {
                    if (entity != null)
                    {
                        context.Set<TSelection>().Remove(entity);
                    }
                }
            }

            context.SaveChanges();
        }
    }
}
