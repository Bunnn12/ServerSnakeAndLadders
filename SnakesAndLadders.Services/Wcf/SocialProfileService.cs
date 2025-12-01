using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using SnakeAndLadders.Contracts.Services;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class SocialProfileService : ISocialProfileService
    {
        private readonly ISocialProfileAppService appService;

        public SocialProfileService(ISocialProfileAppService appService)
        {
            this.appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        public SocialProfileDto[] GetSocialProfiles(int userId)
        {
            var profiles = appService.GetSocialProfiles(userId);
            if (profiles == null)
            {
                return Array.Empty<SocialProfileDto>();
            }

            return profiles.ToArray();
        }

        public SocialProfileDto LinkSocialProfile(LinkSocialProfileRequestDto request)
        {
            return appService.LinkSocialProfile(request);
        }

        public void UnlinkSocialProfile(UnlinkSocialProfileRequestDto request)
        {
            appService.UnlinkSocialProfile(request);
        }
    }
}
