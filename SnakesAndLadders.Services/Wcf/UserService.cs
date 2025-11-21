using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;
using System;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IncludeExceptionDetailInFaults = false)]
    public sealed class UserService : IUserService
    {

        //si
        private readonly IUserAppService _app;

        public UserService(IUserAppService app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public AccountDto GetProfileByUsername(string username)
        {
            return _app.GetProfileByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            return _app.GetProfilePhoto(userId);
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _app.UpdateProfile(request);
        }
    }
}
