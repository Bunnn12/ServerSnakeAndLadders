using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System;
using System.ServiceModel;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = false)]
    public sealed class UserService : IUserService
    {
        private readonly IUserAppService app;

        public UserService(IUserAppService app)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public AccountDto GetProfileByUsername(string username)
        {
            return app.GetProfileByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            return app.GetProfilePhoto(userId);
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return app.UpdateProfile(request);
        }

        public void DeactivateAccount(int userId)
        {
            app.DeactivateAccount(userId);
        }
        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            return app.GetAvatarOptions(userId);
        }
    }
}
