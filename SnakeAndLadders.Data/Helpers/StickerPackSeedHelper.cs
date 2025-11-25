using System;
using System.Collections.Generic;
using System.Linq;
using SnakesAndLadders.Data;

namespace SnakesAndLadders.Data.Helpers
{
    public static class StickerPackSeedHelper
    {
        private static readonly byte[] STATUS_ACTIVE = { 0x01 };

        public static void SeedStickerPacks(Func<SnakeAndLaddersDBEntities1> contextFactory)
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException(nameof(contextFactory));
            }

            using (SnakeAndLaddersDBEntities1 context = contextFactory())
            {
                EnsureStickerPacksExist(context);
                EnsureStickersExist(context);

                context.SaveChanges();
            }
        }

        private static void EnsureStickerPacksExist(SnakeAndLaddersDBEntities1 context)
        {
            var packs = new[]
            {
                new { Code = "STP01", Name = "Paquete Profe \"El Revo\"", Price = 1000 },
                new { Code = "STP02", Name = "Paquete Profe Ocharan",  Price = 800 },
                new { Code = "STP03", Name = "Paquete Profa Liz",      Price = 450 },
                new { Code = "STP04", Name = "Paquete Profe Saul",     Price = 800 },
                new { Code = "STP05", Name = "Paquete Profe Jaime",     Price = 200 },
                new { Code = "STP06", Name = "Paquete Profe Willy",     Price = 400 }
            };

            foreach (var pack in packs)
            {
                bool exists = context.PaqueteStickers
                    .Any(p => p.CodigoPaqueteStickers == pack.Code);

                if (!exists)
                {
                    PaqueteStickers entity = new PaqueteStickers
                    {
                        Nombre = pack.Name,
                        Precio = pack.Price,
                        CodigoPaqueteStickers = pack.Code,
                        Estado = STATUS_ACTIVE
                    };

                    context.PaqueteStickers.Add(entity);
                }
            }
        }

        private static void EnsureStickersExist(SnakeAndLaddersDBEntities1 context)
        {
            List<PaqueteStickers> packs = context.PaqueteStickers.ToList();

            Dictionary<string, int> packIdsByCode = packs
                .GroupBy(p => p.CodigoPaqueteStickers, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().IdPaqueteStickers,
                    StringComparer.OrdinalIgnoreCase);

            var stickers = new[]
            {
                new { Code = "ST000", Name = "Revo happy",           PackCode = "STP01" },
                new { Code = "ST001", Name = "Revo ok",         PackCode = "STP01" },
                new { Code = "ST002", Name = "Revo code compiled",      PackCode = "STP01" },
                new { Code = "ST003", Name = "Revo found bug",      PackCode = "STP01" },
                new { Code = "ST004", Name = "Revo sleppy",      PackCode = "STP01" },
                new { Code = "ST005", Name = "Revo lovely",      PackCode = "STP01" },

                new { Code = "ST037", Name = "Ocharan wave",        PackCode = "STP02" },
                new { Code = "ST038", Name = "Ocharan happy",    PackCode = "STP02" },
                new { Code = "ST039", Name = "Ocharan angry",    PackCode = "STP02" },
                new { Code = "ST040", Name = "Ocharan osito bimbo",    PackCode = "STP02" },
                new { Code = "ST041", Name = "Ocharan bee",    PackCode = "STP02" },
                new { Code = "ST042", Name = "Ocharan panda",    PackCode = "STP02" },
                new { Code = "ST043", Name = "Ocharan butterfly",    PackCode = "STP02" },
                new { Code = "ST044", Name = "Ocharan snake",    PackCode = "STP02" },
                new { Code = "ST045", Name = "Ocharan gruñosito",    PackCode = "STP02" },
                new { Code = "ST046", Name = "Ocharan pumpkin",    PackCode = "STP02" },

                new { Code = "ST023", Name = "Liz wave",            PackCode = "STP03" },
                new { Code = "ST024", Name = "Liz surprised",      PackCode = "STP03" },
                new { Code = "ST025", Name = "Liz paleta payaso",      PackCode = "STP03" },
                new { Code = "ST026", Name = "Liz angry",      PackCode = "STP03" },

                new { Code = "ST018", Name = "Saul wave",      PackCode = "STP04" },
                new { Code = "ST019", Name = "Saul solid principles",    PackCode = "STP04" },
                new { Code = "ST020", Name = "Saul disgusted",    PackCode = "STP04" },
                new { Code = "ST021", Name = "Saul working",    PackCode = "STP04" },
                new { Code = "ST022", Name = "Saul hugging",    PackCode = "STP04" },

                new { Code = "ST006", Name = "Jaime wave",    PackCode = "STP05" },
                new { Code = "ST007", Name = "Jaime absanse limit",    PackCode = "STP05" },
                new { Code = "ST008", Name = "Jaime CIA",    PackCode = "STP05" },
                new { Code = "ST009", Name = "Jaime working",    PackCode = "STP05" },
                new { Code = "ST010", Name = "Jaime mad",    PackCode = "STP05" },
                new { Code = "ST011", Name = "Jaime lovely",    PackCode = "STP05" },
                new { Code = "ST012", Name = "Jaime sad",    PackCode = "STP05" },

                new { Code = "ST013", Name = "Willy wave",    PackCode = "STP06" },
                new { Code = "ST014", Name = "Willy linux",    PackCode = "STP06" },
                new { Code = "ST015", Name = "Willy sad",    PackCode = "STP06" },
                new { Code = "ST016", Name = "Willy angry",    PackCode = "STP06" },
                new { Code = "ST017", Name = "Willy beast mode",    PackCode = "STP06" }
            };

            foreach (var sticker in stickers)
            {
                if (!packIdsByCode.TryGetValue(sticker.PackCode, out int packId))
                {
                    continue;
                }

                bool exists = context.Sticker
                    .Any(s => s.CodigoSticker == sticker.Code);

                if (!exists)
                {
                    Sticker entity = new Sticker
                    {
                        Nombre = sticker.Name,
                        CodigoSticker = sticker.Code,
                        PaqueteStickersIdPaqueteStickers = packId,
                        Estado = STATUS_ACTIVE
                    };

                    context.Sticker.Add(entity);
                }
            }
        }
    }
}
