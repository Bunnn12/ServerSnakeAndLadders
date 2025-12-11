using ServerSnakesAndLadders.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IPasswordRepository
    {
        OperationResult<IReadOnlyList<string>> GetLastPasswordHashes(int userId, int maxCount);

        OperationResult<bool> AddPasswordHash(int userId, string passwordHash);
    }
}
