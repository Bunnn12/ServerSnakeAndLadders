using ServerSnakesAndLadders;
using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using SnakeAndLadders.Infrastructure.Repositories;
using System;

namespace SnakesAndLadders.Host.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService() : this(new UserRepository(new SnakeAndLaddersDBEntities1())) { }
        public UserService(IUserRepository userRepository) => _userRepository = userRepository;

        public AccountDto GetProfileByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.", nameof(username));

            return _userRepository.GetByUsername(username);
        }

        public ProfilePhotoDto GetProfilePhoto(int userId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            return _userRepository.GetPhotoByUserId(userId);
        }
    }
}
