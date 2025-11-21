using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class UserAppService : IUserAppService
    {
        private readonly IUserRepository users;

        public UserAppService(IUserRepository users)
        {
            this.users = users ?? throw new ArgumentNullException(nameof(users));
        }

        public AccountDto GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username is required.", nameof(username));
            }

            return users.GetByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            return users.GetPhotoByUserId(userId);
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            return users.UpdateProfile(request);
        }

        public void DeactivateAccount(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            users.DeactivateUser(userId);
        }
    }
}
