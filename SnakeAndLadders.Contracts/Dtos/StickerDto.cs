using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class StickerDto
    {
        public int StickerId { get; set; }

        public string StickerCode { get; set; }

        public string StickerName { get; set; }
    }
}
