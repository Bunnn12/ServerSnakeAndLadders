using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface ISocialProfileService
    {
        [OperationContract]
        SocialProfileDto[] GetSocialProfiles(int userId);

        [OperationContract]
        SocialProfileDto LinkSocialProfile(LinkSocialProfileRequestDto request);

        [OperationContract]
        void UnlinkSocialProfile(UnlinkSocialProfileRequestDto request);
    }
}
