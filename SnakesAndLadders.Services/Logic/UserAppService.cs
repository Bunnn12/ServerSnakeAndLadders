using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class UserAppService : IUserAppService
    {
        private readonly IUserRepository _users;

        public UserAppService(IUserRepository users)
        {
            if (users == null) throw new ArgumentNullException("users");
            _users = users;
        }

        public AccountDto GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.", "username");

            return _users.GetByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException("userId");
            return _users.GetPhotoByUserId(userId);
        }
        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            return _users.UpdateProfile(request);
        }
    }
}
