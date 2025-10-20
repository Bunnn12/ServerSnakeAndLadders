using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using SnakesAndLadders.Contracts.Interfaces;

namespace SnakesAndLadders.Host.Helpers
{
    /// <summary>
    /// Implementación concreta de IEmailSender usando System.Net.Mail (SMTP).
    /// Lee configuración de AppSettings:
    ///   Smtp:Host, Smtp:Port (por defecto 587), Smtp:EnableSsl (por defecto true),
    ///   Smtp:User, Smtp:Pass, Smtp:From (por defecto = User), Smtp:FromName.
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private static string Get(string key, string fallback)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrEmpty(v) ? fallback : v;
        }

        public void SendVerificationCode(string toEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Destino vacío.", nameof(toEmail));
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Código vacío.", nameof(code));

            var host = Get("Smtp:Host", "");
            var portStr = Get("Smtp:Port", "587");
            var enableSsl = Get("Smtp:EnableSsl", "true");
            var user = Get("Smtp:User", "");
            var pass = Get("Smtp:Pass", "");
            var from = Get("Smtp:From", user);
            var fromName = Get("Smtp:FromName", "Snake & Ladders");

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP no configurado (Smtp:Host).");

            int port; if (!int.TryParse(portStr, out port)) port = 587;
            bool ssl; if (!bool.TryParse(enableSsl, out ssl)) ssl = true;

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(from, fromName);
                msg.To.Add(new MailAddress(toEmail));
                msg.Subject = "Your verification code";
                msg.Body = new StringBuilder()
                    .AppendLine("Hi!")
                    .AppendLine()
                    .AppendLine("Your verification code is: " + code)
                    .AppendLine("This code expires in 10 minutes.")
                    .ToString();
                msg.IsBodyHtml = false;

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl = ssl;

                    if (!string.IsNullOrWhiteSpace(user))
                        smtp.Credentials = new NetworkCredential(user, pass);
                    else
                        smtp.UseDefaultCredentials = true;

                    // Forzar TLS razonable en .NET Framework
                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    smtp.Send(msg);
                }
            }
        }
    }
}
