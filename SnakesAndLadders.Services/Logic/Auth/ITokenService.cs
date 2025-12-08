using System;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public interface ITokenService
    {
        string IssueToken(int userId, DateTime expiresAtUtc);
        int GetUserIdFromToken(string token);
    }
}
