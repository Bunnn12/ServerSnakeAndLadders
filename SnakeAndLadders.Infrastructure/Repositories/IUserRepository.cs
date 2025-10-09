using System.Data.Entity.Infrastructure;

using SnakeAndLadders.Contracts.Dtos;


namespace SnakeAndLadders.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        AccountDto GetByUsername(string username);
        ProfilePhotoDto GetPhotoByUserId(int userId);
    }
}
