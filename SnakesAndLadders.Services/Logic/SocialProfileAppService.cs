using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class SocialProfileAppService : ISocialProfileAppService
    {
        private readonly ISocialProfileRepository socialProfiles;
        private readonly IUserRepository users;

        public SocialProfileAppService(
            ISocialProfileRepository socialProfiles,
            IUserRepository users)
        {
            this.socialProfiles = socialProfiles ?? throw new ArgumentNullException(nameof(socialProfiles));
            this.users = users ?? throw new ArgumentNullException(nameof(users));
        }

        public IReadOnlyList<SocialProfileDto> GetSocialProfiles(int userId)
        {
            EnsureUserExists(userId);
            return socialProfiles.GetByUserId(userId);
        }

        public SocialProfileDto LinkSocialProfile(LinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureUserExists(request.UserId);
            return socialProfiles.Upsert(request);
        }

        public void UnlinkSocialProfile(UnlinkSocialProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureUserExists(request.UserId);
            socialProfiles.Delete(request);
        }

        private void EnsureUserExists(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            var account = users.GetByUserId(userId);

            if (account == null)
            {
                throw new InvalidOperationException("User not found.");
            }
        }
    }
}
