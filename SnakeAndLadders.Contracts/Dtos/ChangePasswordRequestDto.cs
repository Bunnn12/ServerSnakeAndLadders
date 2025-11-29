using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class ChangePasswordRequestDto
    {
        public string Email { get; set; }

        public string NewPassword { get; set; }

        public string VerificationCode { get; set; }
    }
}
