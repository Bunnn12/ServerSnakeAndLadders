using SnakeAndLadders.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    public sealed class ItemEffectResult
    {
        public string ItemCode { get; set; }

        public ItemEffectType EffectType { get; set; }

        public int UserId { get; set; }

        public int? TargetUserId { get; set; }

        public int? FromCellIndex { get; set; }

        public int? ToCellIndex { get; set; }

        public bool WasBlockedByShield { get; set; }

        public bool TargetFrozen { get; set; }

        public bool ShieldActivated { get; set; }

        public bool ShouldConsumeItemImmediately { get; set; }
    }
}
