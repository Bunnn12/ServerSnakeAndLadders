using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = false)]
    public sealed class MatchInvitationService : IMatchInvitationService
    {
        private readonly IMatchInvitationAppService appService;

        public MatchInvitationService(IMatchInvitationAppService appService)
        {
            this.appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        public OperationResult InviteFriendToGame(InviteFriendToGameRequestDto request)
        {
            return appService.InviteFriendToGame(request);
        }
    }
}
