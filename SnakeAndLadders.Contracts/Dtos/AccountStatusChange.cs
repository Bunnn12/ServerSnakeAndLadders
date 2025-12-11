using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Dtos
{
    public class AccountStatusChange
    {
        public int UserId { get; }
        public bool IsActive { get; }
        public byte[] Status { get; }

        public AccountStatusChange(int userId, bool isActive, byte[] status)
        {
            UserId = userId;
            IsActive = isActive;
            Status = status;
        }
    }
}
