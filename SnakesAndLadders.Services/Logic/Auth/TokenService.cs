using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class TokenService : ITokenService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(TokenService));

        private const int InvalidUserId = 0;
        private const int MinValidUserId = 1;
        private const long MinValidUnixTimestamp = 1;

        private const int TokenPartsExpectedLength = 3;
        private const int TokenUserIdIndex = 0;
        private const int TokenExpIndex = 1;
        private const int TokenSignatureIndex = 2;

        private const string AppKeySecret = "Auth:Secret";

        public string IssueToken(int userId, DateTime expiresAtUtc)
        {
            string secret = ConfigurationManager.AppSettings[AppKeySecret];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Auth secret is not configured (Auth:Secret).");
            }

            long expUnix = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
            string payload = $"{userId}|{expUnix}";
            string signatureHex = ComputeHmacHex(secret, payload);
            string raw = $"{payload}|{signatureHex}";

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public int GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return InvalidUserId;
            }

            if (int.TryParse(token, out int userIdCompat))
            {
                return userIdCompat >= MinValidUserId ? userIdCompat : InvalidUserId;
            }

            try
            {
                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = raw.Split('|');

                if (parts.Length != TokenPartsExpectedLength)
                {
                    return InvalidUserId;
                }

                if (!int.TryParse(parts[TokenUserIdIndex], out int userId) || userId < MinValidUserId)
                {
                    return InvalidUserId;
                }

                if (!long.TryParse(parts[TokenExpIndex], out long expUnix) || expUnix < MinValidUnixTimestamp)
                {
                    return InvalidUserId;
                }

                long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (nowUnix > expUnix)
                {
                    return InvalidUserId;
                }

                string secret = ConfigurationManager.AppSettings[AppKeySecret] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(secret))
                {
                    return InvalidUserId;
                }

                string expected = ComputeHmacHex(secret, $"{userId}|{expUnix}");

                return string.Equals(expected, parts[TokenSignatureIndex], StringComparison.OrdinalIgnoreCase)
                    ? userId
                    : InvalidUserId;
            }
            catch (FormatException ex)
            {
                _logger.Error("Format error while validating auth token.", ex);
                return InvalidUserId;
            }
            catch (ConfigurationErrorsException ex)
            {
                _logger.Error("Configuration error while validating auth token.", ex);
                return InvalidUserId;
            }
            catch (CryptographicException ex)
            {
                _logger.Error("Cryptographic error while validating auth token.", ex);
                return InvalidUserId;
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while validating auth token.", ex);
                return InvalidUserId;
            }
        }

        private static string ComputeHmacHex(string secret, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var stringBuilder = new StringBuilder(bytes.Length * 2);

                for (int index = 0; index < bytes.Length; index++)
                {
                    stringBuilder.Append(bytes[index].ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}
