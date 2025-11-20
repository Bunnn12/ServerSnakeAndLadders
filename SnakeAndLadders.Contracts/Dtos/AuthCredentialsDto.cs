using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class AuthCredentialsDto
    {
        public int UserId { get; set; }
        public string PasswordHash { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePhotoId { get; set; }
    }
}
