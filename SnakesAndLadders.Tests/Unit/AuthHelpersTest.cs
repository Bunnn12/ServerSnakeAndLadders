using System;
using System.Configuration;
using SnakesAndLadders.Services.Logic.Auth;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class AuthHelpersTests
    {
        private const int INVALID_USER_ID = 0;
        private const int VALID_USER_ID = 10;

        private const string SECRET_APP_KEY = "Auth:Secret";
        private const string SECRET_VALUE = "unit-test-secret";
        private const string OTHER_SECRET_VALUE = "unit-test-secret-2";


        [Fact]
        public void TestIssueTokenThrowsInvalidOperationWhenSecretMissing()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = " ";

            var service = new TokenService();

            var ex = Assert.Throws<InvalidOperationException>(
                () => service.IssueToken(VALID_USER_ID, DateTime.UtcNow.AddMinutes(5)));

            bool isOk =
                ex.Message != null &&
                ex.Message.Contains("Auth:Secret");

            Assert.True(isOk);
        }

        [Fact]
        public void TestIssueTokenAndGetUserIdFromTokenReturnsUserIdWhenTokenIsValid()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(10);

            string token = service.IssueToken(VALID_USER_ID, expiresAtUtc);

            int userId = service.GetUserIdFromToken(token);

            bool isOk = userId == VALID_USER_ID;

            Assert.True(isOk);
        }



        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestGetUserIdFromTokenReturnsInvalidUserIdWhenTokenIsNullOrWhitespace(
            string token)
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            int userId = service.GetUserIdFromToken(token);

            bool isOk = userId == INVALID_USER_ID;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetUserIdFromTokenReturnsUserIdWhenTokenIsPlainNumberCompat()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            int userId = service.GetUserIdFromToken("15");

            bool isOk = userId == 15;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetUserIdFromTokenReturnsInvalidUserIdWhenTokenExpired()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            DateTime expiredAtUtc = DateTime.UtcNow.AddMinutes(-5);

            string token = service.IssueToken(VALID_USER_ID, expiredAtUtc);

            int userId = service.GetUserIdFromToken(token);

            bool isOk = userId == INVALID_USER_ID;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetUserIdFromTokenReturnsInvalidUserIdWhenSignatureDoesNotMatch()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            string token = service.IssueToken(
                VALID_USER_ID,
                DateTime.UtcNow.AddMinutes(10));

            ConfigurationManager.AppSettings[SECRET_APP_KEY] = OTHER_SECRET_VALUE;

            int userId = service.GetUserIdFromToken(token);

            bool isOk = userId == INVALID_USER_ID;

            Assert.True(isOk);
        }

        [Fact]
        public void TestGetUserIdFromTokenReturnsInvalidUserIdWhenTokenIsNotBase64()
        {
            ConfigurationManager.AppSettings[SECRET_APP_KEY] = SECRET_VALUE;
            var service = new TokenService();

            int userId = service.GetUserIdFromToken("this-is-not-base64");

            bool isOk = userId == INVALID_USER_ID;

            Assert.True(isOk);
        }


        private static string CreateRandomEmail()
        {
            return Guid.NewGuid().ToString("N") + "@example.com";
        }

        [Fact]
        public void TestVerificationCodeStoreSaveNewCodeAndTryGetStoresEntry()
        {
            var store = new VerificationCodeStore();
            string email = CreateRandomEmail();
            string code = "123456";
            DateTime nowUtc = DateTime.UtcNow;

            store.SaveNewCode(email, code, nowUtc);

            bool exists = store.TryGet(email, out VerificationCodeEntry entry);

            bool isOk =
                exists &&
                entry != null &&
                entry.Code == code &&
                entry.FailedAttempts == 0 &&
                entry.LastSentUtc >= nowUtc &&
                entry.ExpiresUtc > nowUtc;

            Assert.True(isOk);
        }

        [Fact]
        public void TestVerificationCodeStoreRemoveDeletesEntry()
        {
            var store = new VerificationCodeStore();
            string email = CreateRandomEmail();

            store.SaveNewCode(email, "111111", DateTime.UtcNow);

            store.Remove(email);

            bool existsAfter = store.TryGet(email, out VerificationCodeEntry _);

            bool isOk = !existsAfter;

            Assert.True(isOk);
        }

        [Fact]
        public void TestVerificationCodeStoreRegisterFailedAttemptIncrementsAndRemovesAtMax()
        {
            const int MAX_FAILED_ATTEMPTS = 5;

            var store = new VerificationCodeStore();
            string email = CreateRandomEmail();

            store.SaveNewCode(email, "999999", DateTime.UtcNow);

            bool isOk = true;

            bool existsFirst = store.TryGet(email, out VerificationCodeEntry entry);
            isOk = isOk && existsFirst && entry.FailedAttempts == 0;

            for (int attempt = 1; attempt < MAX_FAILED_ATTEMPTS; attempt++)
            {
                entry = store.RegisterFailedAttempt(email, entry);

                bool exists = store.TryGet(email, out VerificationCodeEntry current);
                isOk = isOk &&
                    exists &&
                    current.FailedAttempts == attempt;

                entry = current;
            }

            entry = store.RegisterFailedAttempt(email, entry);

            bool existsAfterMax =
                store.TryGet(email, out VerificationCodeEntry _);

            isOk = isOk && !existsAfterMax;

            Assert.True(isOk);
        }


    }
}
