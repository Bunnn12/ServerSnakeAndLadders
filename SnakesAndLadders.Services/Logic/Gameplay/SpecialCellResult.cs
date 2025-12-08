using System;

namespace SnakesAndLadders.Services.Logic.Gameplay
{
    internal sealed class SpecialCellResult
    {
        public int FinalCellIndex { get; set; }
        public string ExtraInfo { get; set; }
        public int? MessageIndex { get; set; }
        public bool GrantsExtraRoll { get; set; }
        public string GrantedItemCode { get; set; }
        public string GrantedDiceCode { get; set; }

        public SpecialCellResult()
        {
            FinalCellIndex = 0;
            ExtraInfo = string.Empty;
            MessageIndex = null;
            GrantsExtraRoll = false;
            GrantedItemCode = null;
            GrantedDiceCode = null;
        }
    }
}
