using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakesAndLadders.Services.Logic;     // IUserAppService
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
    }

}
