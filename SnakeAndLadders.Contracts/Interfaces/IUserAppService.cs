using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Dtos;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IUserAppService
    {
        AccountDto GetProfileByUsername(string username);
        ProfilePhotoDto GetProfilePhoto(int userId);
        AccountDto UpdateProfile(UpdateProfileRequestDto request);
        void DeactivateAccount(int userId);
        AvatarProfileOptionsDto GetAvatarOptions(int userId);
    }
}
