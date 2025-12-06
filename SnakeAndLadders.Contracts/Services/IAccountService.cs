using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

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
