using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface ISocialProfileAppService
    {
        IReadOnlyList<SocialProfileDto> GetSocialProfiles(int userId);

        SocialProfileDto LinkSocialProfile(LinkSocialProfileRequestDto request);

        void UnlinkSocialProfile(UnlinkSocialProfileRequestDto request);
    }
}
