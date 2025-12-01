using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;
using System;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class UserAppService : IUserAppService
    {
        private readonly IUserRepository users;
        private readonly IAccountStatusRepository accountStatusRepository;

        public UserAppService(IUserRepository users, IAccountStatusRepository accountStatusRepository)
        {
            this.users = users ?? throw new ArgumentNullException(nameof(users));
            this.accountStatusRepository = accountStatusRepository 
                ?? throw new ArgumentNullException(nameof(accountStatusRepository));
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

            accountStatusRepository.SetUserAndAccountActiveState(userId, isActive: false);
        }
        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            return users.GetAvatarOptions(userId);
        }
    }
}
