using System;
using log4net;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Services.Logic
{
    public sealed class UserAppService : IUserAppService
    {
        private const string ERROR_USERNAME_REQUIRED = "UserName is required.";
        private const string ERROR_REQUEST_REQUIRED = "Request is required.";
        private const string ERROR_USER_ID_POSITIVE = "UserId must be positive.";

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
            ValidateRequiredString(username, nameof(username), ERROR_USERNAME_REQUIRED);

            return _userRepository.GetByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            ValidateUserId(userId);

            return _userRepository.GetPhotoByUserId(userId);
        }

        public AccountDto UpdateProfile(UpdateProfileRequestDto request)
        {
            ValidateRequest(request, nameof(request));

            return _userRepository.UpdateProfile(request);
        }

        public void DeactivateAccount(int userId)
        {
            ValidateUserId(userId);

            _accountStatusRepository.DeactivateUserAndAccount(userId);
        }

        public AvatarProfileOptionsDto GetAvatarOptions(int userId)
        {
            ValidateUserId(userId);

            return _userRepository.GetAvatarOptions(userId);
        }

        public AccountDto SelectAvatarForProfile(AvatarSelectionRequestDto request)
        {
            ValidateRequest(request, nameof(request));

            return _userRepository.SelectAvatarForProfile(request);
        }
        private static void ValidateUserId(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), ERROR_USER_ID_POSITIVE);
            }
        }

        private static void ValidateRequest<TRequest>(TRequest request, string paramName)
            where TRequest : class
        {
            if (request == null)
            {
                throw new ArgumentNullException(paramName, ERROR_REQUEST_REQUIRED);
            }
        }

        private static void ValidateRequiredString(string value, string paramName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(errorMessage, paramName);
            }
        }
    }
}
