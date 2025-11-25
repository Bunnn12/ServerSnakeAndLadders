using System.ServiceModel;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Services
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        AccountDto GetProfileByUsername(string username);

        [OperationContract]
        ProfilePhotoDto GetProfilePhoto(int userId);

        [OperationContract]
        AccountDto UpdateProfile(UpdateProfileRequestDto request);  

        [OperationContract]
        void DeactivateAccount(int userId);
    }
}
