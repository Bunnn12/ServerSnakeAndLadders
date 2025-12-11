using SnakeAndLadders.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Data.Interfaces
{
    public interface IFriendSearchRepository
    {
        IReadOnlyList<UserBriefDto> SearchUsers(string query, int maxResults, int excludeUserId);
    }
}
