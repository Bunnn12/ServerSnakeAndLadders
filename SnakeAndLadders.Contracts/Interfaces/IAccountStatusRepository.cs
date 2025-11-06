using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLadders.Contracts.Interfaces
{
    public interface IAccountStatusRepository
    {
        void SetUserAndAccountActiveState(int userId, bool isActive);
    }
}
