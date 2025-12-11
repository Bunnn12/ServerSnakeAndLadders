using ServerSnakesAndLadders.Common;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Services.Constants;
using System.Collections.Generic;

namespace SnakesAndLadders.Services.Logic.Auth
{
    internal static class AuthResultFactory
    {
        internal static AuthResult Ok()
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthConstants.AuthCodeOk,
                Meta = new Dictionary<string, string>()
            };
        }

        internal static AuthResult OkWithUser(int userId, string displayName)
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthConstants.AuthCodeOk,
                Meta = new Dictionary<string, string>(),
                UserId = userId,
                DisplayName = displayName
            };
        }

        internal static AuthResult OkWithUserProfile(
            int userId,
            string displayName,
            string profilePhotoId)
        {
            return new AuthResult
            {
                Success = true,
                Code = AuthConstants.AuthCodeOk,
                Meta = new Dictionary<string, string>(),
                UserId = userId,
                DisplayName = displayName,
                ProfilePhotoId = profilePhotoId
            };
        }

        internal static AuthResult OkWithCustomCode(string code, int userId)
        {
            return new AuthResult
            {
                Success = true,
                Code = code,
                Meta = new Dictionary<string, string>(),
                UserId = userId
            };
        }

        internal static AuthResult Fail(string code, Dictionary<string, string> meta = null)
        {
            return new AuthResult
            {
                Success = false,
                Code = code,
                Meta = meta ?? new Dictionary<string, string>()
            };
        }

        internal static AuthResult FailWithErrorType(string code, string errorType)
        {
            var meta = new Dictionary<string, string>
            {
                [AuthConstants.MetaKeyErrorType] = errorType
            };

            return Fail(code, meta);
        }
    }
}
