using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public sealed class AvatarSelectionRequestDto
    {
        public int UserId { get; set; }

        public string AvatarCode { get; set; } = string.Empty;
    }
}
