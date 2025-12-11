using System;
using System.IO;
using SnakeAndLadders.Contracts.Dtos;
using SnakesAndLadders.Host.Helpers;
using Xunit;

namespace SnakesAndLadders.Tests.Unit
{
    public sealed class SmtpEmailSenderTests : IDisposable
    {
        private const string SecretsFileName = "ServerSecrets.config";

        private const string VALID_EMAIL = "user@example.com";
        private const string VALID_CODE = "123456";
        private const string VALID_GAME_CODE = "999999";

        private readonly string _secretsPath;

        public SmtpEmailSenderTests()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _secretsPath = Path.Combine(baseDirectory, SecretsFileName);

            DeleteSecretsFileIfExists();
        }

        public void Dispose()
        {
            DeleteSecretsFileIfExists();
        }

        #region SendVerificationCode – parameter validation

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestSendVerificationCodeThrowsWhenEmailIsNullOrWhitespace(
            string email)
        {
            var sender = new SmtpEmailSender();

            var ex = Assert.Throws<ArgumentException>(
                () => sender.SendVerificationCode(email, VALID_CODE));

            Assert.Equal("email", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestSendVerificationCodeThrowsWhenCodeIsNullOrWhitespace(
            string code)
        {
            var sender = new SmtpEmailSender();

            var ex = Assert.Throws<ArgumentException>(
                () => sender.SendVerificationCode(VALID_EMAIL, code));

            Assert.Equal("code", ex.ParamName);
        }

        #endregion

        #region SendVerificationCode – configuration errors

        [Fact]
        public void TestSendVerificationCodeThrowsWhenSmtpHostIsNotConfigured()
        {
            var sender = new SmtpEmailSender();

            DeleteSecretsFileIfExists();

            Exception ex = Assert.ThrowsAny<Exception>(
                () => sender.SendVerificationCode(VALID_EMAIL, VALID_CODE));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestSendVerificationCodeThrowsWhenCredentialsAreNotConfigured()
        {
            var sender = new SmtpEmailSender();

            WriteSecretsFile(@"
                <configuration>
                  <appSettings>
                    <add key=""Smtp:Host"" value=""smtp.example.com"" />
                    <add key=""Smtp:User"" value="""" />
                    <add key=""Smtp:Pass"" value="""" />
                    <add key=""Smtp:From"" value=""noreply@example.com"" />
                  </appSettings>
                </configuration>");

            var ex = Assert.Throws<InvalidOperationException>(
                () => sender.SendVerificationCode(VALID_EMAIL, VALID_CODE));

            Assert.Equal("SMTP credentials are not configured.", ex.Message);
        }

        #endregion

        #region SendGameInvitation – parameter validation

        [Fact]
        public void TestSendGameInvitationThrowsWhenRequestIsNull()
        {
            var sender = new SmtpEmailSender();

            var ex = Assert.Throws<ArgumentNullException>(
                () => sender.SendGameInvitation(null));

            Assert.Equal("request", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestSendGameInvitationThrowsWhenToEmailIsNullOrWhitespace(
            string email)
        {
            var sender = new SmtpEmailSender();

            var request = new GameInvitationEmailDto
            {
                ToEmail = email,
                GameCode = VALID_GAME_CODE
            };

            var ex = Assert.Throws<ArgumentException>(
                () => sender.SendGameInvitation(request));

            Assert.Equal("ToEmail", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TestSendGameInvitationThrowsWhenGameCodeIsNullOrWhitespace(
            string gameCode)
        {
            var sender = new SmtpEmailSender();

            var request = new GameInvitationEmailDto
            {
                ToEmail = VALID_EMAIL,
                GameCode = gameCode
            };

            var ex = Assert.Throws<ArgumentException>(
                () => sender.SendGameInvitation(request));

            Assert.Equal("GameCode", ex.ParamName);
        }

        #endregion

        #region SendGameInvitation – configuration errors

        [Fact]
        public void TestSendGameInvitationThrowsWhenSmtpHostIsNotConfigured()
        {
            var sender = new SmtpEmailSender();

            DeleteSecretsFileIfExists();

            var request = new GameInvitationEmailDto
            {
                ToEmail = VALID_EMAIL,
                GameCode = VALID_GAME_CODE
            };

            Exception ex = Assert.ThrowsAny<Exception>(
                () => sender.SendGameInvitation(request));

            Assert.NotNull(ex);
        }

        [Fact]
        public void TestSendGameInvitationThrowsWhenCredentialsAreNotConfigured()
        {
            var sender = new SmtpEmailSender();

            WriteSecretsFile(@"
                <configuration>
                  <appSettings>
                    <add key=""Smtp:Host"" value=""smtp.example.com"" />
                    <add key=""Smtp:User"" value="""" />
                    <add key=""Smtp:Pass"" value="""" />
                    <add key=""Smtp:From"" value=""noreply@example.com"" />
                  </appSettings>
                </configuration>");

            var request = new GameInvitationEmailDto
            {
                ToEmail = VALID_EMAIL,
                GameCode = VALID_GAME_CODE
            };

            var ex = Assert.Throws<InvalidOperationException>(
                () => sender.SendGameInvitation(request));

            Assert.Equal("SMTP credentials are not configured.", ex.Message);
        }

        #endregion
        //aaa
        #region Helpers

        private void DeleteSecretsFileIfExists()
        {
            if (File.Exists(_secretsPath))
            {
                File.Delete(_secretsPath);
            }
        }

        private void WriteSecretsFile(string xmlContent)
        {
            File.WriteAllText(_secretsPath, xmlContent.Trim());
        }

        #endregion
    }
}
