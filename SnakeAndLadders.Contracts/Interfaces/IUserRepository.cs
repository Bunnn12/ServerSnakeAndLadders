using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IUserRepository
    {
        AccountDto GetByUsername(string username);
        ProfilePhotoDto GetPhotoByUserId(int userId);
        AccountDto UpdateProfile(UpdateProfileRequestDto request);

        void DeactivateUser(int userId);
    }
}
