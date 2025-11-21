using SnakeAndLadders.Contracts.Dtos;

public interface IUserAppService
{
    AccountDto GetProfileByUsername(string username);
    ProfilePhotoDto GetProfilePhoto(int userId);
    AccountDto UpdateProfile(UpdateProfileRequestDto request);
    void DeactivateAccount(int userId);
}
