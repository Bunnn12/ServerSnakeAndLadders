using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface ISocialProfileRepository
    {
        IReadOnlyList<SocialProfileDto> GetByUserId(int userId);

        SocialProfileDto Upsert(LinkSocialProfileRequestDto request);

        void Delete(UnlinkSocialProfileRequestDto request);
    }
}
