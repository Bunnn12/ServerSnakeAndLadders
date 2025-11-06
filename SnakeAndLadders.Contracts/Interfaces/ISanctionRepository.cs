using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface ISanctionRepository
    {
        void InsertSanction(SanctionDto dto);

        SanctionDto GetLastSanctionForUser(int userId);

        IList<SanctionDto> GetSanctionsHistory(int userId);
    }
}
