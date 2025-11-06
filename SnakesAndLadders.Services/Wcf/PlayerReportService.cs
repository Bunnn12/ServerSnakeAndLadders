using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class PlayerReportService : IPlayerReportService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlayerReportService));

        private readonly IPlayerReportAppService appService;

        public PlayerReportService(IPlayerReportAppService appServiceValue)
        {
            appService = appServiceValue ?? throw new ArgumentNullException(nameof(appServiceValue));
        }

        public void CreateReport(ReportDto report)
        {
            appService.CreateReport(report);
        }

        public BanInfoDto GetCurrentBan(int userId)
        {
            return appService.GetCurrentBan(userId);
        }

        public IEnumerable<SanctionDto> GetSanctionsHistory(int userId)
        {
            return appService.GetSanctionsHistory(userId);
        }

        public void QuickKickPlayer(QuickKickDto quickKick)
        {
            appService.QuickKickPlayer(quickKick);
        }
    }
}
