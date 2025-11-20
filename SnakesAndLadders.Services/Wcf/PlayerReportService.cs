using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class PlayerReportService : IPlayerReportService
    {
        private readonly IPlayerReportAppService _playerReportAppService;

        public PlayerReportService(IPlayerReportAppService playerReportAppService)
        {
            _playerReportAppService = playerReportAppService
                ?? throw new ArgumentNullException(nameof(playerReportAppService));
        }

        public void CreateReport(ReportDto report)
        {
            _playerReportAppService.CreateReport(report);
        }

        public BanInfoDto GetCurrentBan(int userId)
        {
            return _playerReportAppService.GetCurrentBan(userId);
        }

        public IEnumerable<SanctionDto> GetSanctionsHistory(int userId)
        {
            return _playerReportAppService.GetSanctionsHistory(userId);
        }

        public void QuickKickPlayer(QuickKickDto quickKick)
        {
            _playerReportAppService.QuickKickPlayer(quickKick);
        }
    }
}
