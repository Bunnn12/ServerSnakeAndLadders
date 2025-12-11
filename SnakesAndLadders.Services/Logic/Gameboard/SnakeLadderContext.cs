using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Helpers;
using SnakesAndLadders.Services.Constants;

namespace SnakesAndLadders.Services.Logic.Gameboard
{
    public sealed class SnakeLadderContext
    {
        public SnakeLadderContext(
            IList<BoardCellDto> cells,
            IList<BoardLinkDto> links,
            HashSet<int> usedIndexes,
            int totalCells)
        {
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Links = links ?? throw new ArgumentNullException(nameof(links));
            UsedIndexes = usedIndexes ?? throw new ArgumentNullException(nameof(usedIndexes));
            TotalCells = totalCells;

            CellByIndex = cells.ToDictionary(c => c.Index);
        }

        public IList<BoardCellDto> Cells { get; }

        public IList<BoardLinkDto> Links { get; }

        public HashSet<int> UsedIndexes { get; }

        public int TotalCells { get; }

        public IDictionary<int, BoardCellDto> CellByIndex { get; }
    }
}
