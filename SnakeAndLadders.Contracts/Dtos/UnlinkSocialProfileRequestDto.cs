using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLadders.Contracts.Enums;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class UnlinkSocialProfileRequestDto
    {
        public int UserId { get; set; }

        public SocialNetworkType Network { get; set; }
    }
}
