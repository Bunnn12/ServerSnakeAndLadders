using SnakeAndLadders.Contracts.Dtos;


namespace SnakesAndLadders.Services.Logic
{
    public interface IUserAppService
    {
        AccountDto GetProfileByUsername(string username);
        ProfilePhotoDto GetProfilePhoto(int userId);
    }
}

