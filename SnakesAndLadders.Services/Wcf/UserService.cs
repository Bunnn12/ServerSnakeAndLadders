using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;    
using System;
using System.ServiceModel;

namespace SnakesAndLadders.Services.Wcf
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = false)]
    public sealed class UserService : IUserService
    {
        private readonly IUserAppService _app;
        public UserService(IUserAppService app) { _app = app; }

        public AccountDto GetProfileByUsername(string username) => _app.GetProfileByUsername(username);
        public ProfilePhotoDto GetProfilePhoto(int userId) => _app.GetProfilePhoto(userId);

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            try
            {
                return _app.UpdateProfile(request);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}



