using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    public interface IPlayerReportAppService
    {
        void CreateReport(ReportDto report);
        BanInfoDto GetCurrentBan(int userId);
        IList<SanctionDto> GetSanctionsHistory(int userId);
        void QuickKickPlayer(QuickKickDto quickKick);
    }
}
