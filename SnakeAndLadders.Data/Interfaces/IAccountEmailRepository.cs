using ServerSnakesAndLadders.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IAccountEmailRepository
    {
        OperationResult<string> GetEmailByUserId(int userId);
    }
}
