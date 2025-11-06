using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Faults;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IPlayerReportService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void CreateReport(ReportDto report);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BanInfoDto GetCurrentBan(int userId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        IEnumerable<SanctionDto> GetSanctionsHistory(int userId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void QuickKickPlayer(QuickKickDto quickKick);
    }
}
