using System;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class UserAppService : IUserAppService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserAppService));

        private readonly IUserRepository _userRepository;
        private readonly IAccountStatusRepository _accountStatusRepository;

        public UserAppService(
            IUserRepository userRepository,
            IAccountStatusRepository accountStatusRepository)
        {
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _accountStatusRepository = accountStatusRepository
                ?? throw new ArgumentNullException(nameof(accountStatusRepository));
        }

        public AccountDto GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("UserName is required.", nameof(username));
            }

            return _userRepository.GetByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            return _userRepository.GetPhotoByUserId(userId);
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _userRepository.UpdateProfile(request);
        }

        public void DeactivateAccount(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            _accountStatusRepository.SetUserAndAccountActiveState(userId, isActive: false);
        }

        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            return _userRepository.GetAvatarOptions(userId);
        }

        public AccountDto SelectAvatarForProfile(AvatarSelectionRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                return _userRepository.SelectAvatarForProfile(request);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while selecting avatar for profile.", ex);
                throw;
            }
        }
    }
}
