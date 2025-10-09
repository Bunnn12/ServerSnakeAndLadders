using SnakeAndLadders.Contracts.Dtos;
using SnakeAndLadders.Contracts.Services;
using System.ServiceModel;

namespace SnakeAndLadders.Contracts
{
    [ServiceContract]
    public interface IAccountService
    {
        [OperationContract]
        AccountDto GetAccountById(int userId);

        [OperationContract]
        AccountDto GetAccountByUsername(string username);

        [OperationContract]
        ProfilePhotoDto GetProfilePhoto(int userId);
    }
}
