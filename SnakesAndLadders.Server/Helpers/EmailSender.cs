using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using log4net;
using SnakeAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Host.Helpers
{
    /// <summary>
    /// SMTP-based implementation of IEmailSender for sending verification emails.
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private const string SMTP_HOST_KEY = "Smtp:Host";
        private const string SMTP_PORT_KEY = "Smtp:Port";
        private const string SMTP_ENABLE_SSL_KEY = "Smtp:EnableSsl";
        private const string SMTP_USER_KEY = "Smtp:User";
        private const string SMTP_PASS_KEY = "Smtp:Pass";
        private const string SMTP_FROM_KEY = "Smtp:From";
        private const string SMTP_FROM_NAME_KEY = "Smtp:FromName";

        private const int DEFAULT_SMTP_PORT = 587;
        private const bool DEFAULT_ENABLE_SSL = true;
        private const string DEFAULT_FROM_NAME = "Snakes & Ladders";
        private const string SECRET_FILE_NAME = "ServerSecrets.config";

        private const string EMAIL_SUBJECT_VERIFICATION = "Your verification code";
        private const string EMAIL_GREETING = "Hi!";
        private const string EMAIL_CODE_PREFIX = "Your verification code is: ";
        private const string EMAIL_EXPIRATION_TEXT = "This code expires in 10 minutes.";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(SmtpEmailSender));

        public void SendVerificationCode(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Destination email is required.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Verification code is required.", nameof(code));
            }

            string host = GetAppSetting(SMTP_HOST_KEY, string.Empty);
            string portText = GetAppSetting(SMTP_PORT_KEY, DEFAULT_SMTP_PORT.ToString());
            string enableSslText = GetAppSetting(
                SMTP_ENABLE_SSL_KEY,
                DEFAULT_ENABLE_SSL.ToString());

            string user = GetAppSetting(SMTP_USER_KEY, string.Empty);
            string pass = GetAppSetting(SMTP_PASS_KEY, string.Empty);
            string from = GetAppSetting(SMTP_FROM_KEY, user);
            string fromName = GetAppSetting(SMTP_FROM_NAME_KEY, DEFAULT_FROM_NAME);

            if (string.IsNullOrWhiteSpace(host))
            {
                Logger.Error("SMTP host is not configured (Smtp:Host).");
                throw new InvalidOperationException("SMTP host is not configured.");
            }

            if (!int.TryParse(portText, out int port))
            {
                port = DEFAULT_SMTP_PORT;
            }

            if (!bool.TryParse(enableSslText, out bool enableSsl))
            {
                enableSsl = DEFAULT_ENABLE_SSL;
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from, fromName);
                message.To.Add(new MailAddress(email));
                message.Subject = EMAIL_SUBJECT_VERIFICATION;
                message.Body = BuildBody(code);
                message.IsBodyHtml = false;

                using (var smtpClient = new SmtpClient(host, port))
                {
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.UseDefaultCredentials = false;

                    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    {
                        Logger.Error("SMTP credentials are not configured (Smtp:User / Smtp:Pass).");
                        throw new InvalidOperationException("SMTP credentials are not configured.");
                    }

                    smtpClient.Credentials = new NetworkCredential(user, pass);

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    Logger.InfoFormat(
                        "SMTP config: host={0}, port={1}, enableSsl={2}, user={3}",
                        host,
                        port,
                        smtpClient.EnableSsl,
                        user);

                    try
                    {
                        smtpClient.Send(message);
                    }
                    catch (SmtpException ex)
                    {
                        Logger.Error("SMTP error while sending verification email.", ex);

                        throw new InvalidOperationException(
                            "SMTP error while sending verification email.",
                            ex);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Unexpected error while sending verification email.",
                            ex);
                    }
                }
            }
        }

        private static string BuildBody(string code)
        {
            var builder = new StringBuilder();

            builder.AppendLine(EMAIL_GREETING);
            builder.AppendLine();
            builder.AppendLine(EMAIL_CODE_PREFIX + code);
            builder.AppendLine(EMAIL_EXPIRATION_TEXT);

            return builder.ToString();
        }

        private static string GetAppSetting(string key, string fallback)
        {
            string secretValue = GetSecretAppSetting(key);
            if (!string.IsNullOrWhiteSpace(secretValue))
            {
                return secretValue;
            }

            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string GetSecretAppSetting(string key)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string secretsPath = Path.Combine(baseDirectory, SECRET_FILE_NAME);

                Logger.InfoFormat("SMTP secrets path: {0}", secretsPath);

                if (!File.Exists(secretsPath))
                {
                    Logger.Warn("SMTP secrets file not found.");
                    return null;
                }

                var fileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = secretsPath
                };

                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(
                    fileMap,
                    ConfigurationUserLevel.None);

                var setting = config.AppSettings.Settings[key];
                return setting?.Value;
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.Warn("Failed to read SMTP secrets configuration.", ex);
                return null;
            }
        }
    }
}
