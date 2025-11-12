using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class UserBriefDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ProfilePhotoId { get; set; }
    }
}
