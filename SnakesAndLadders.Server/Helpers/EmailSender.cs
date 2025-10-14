using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Text;

namespace SnakeAndLadders.Host.Helpers
{
    internal static class EmailSender
    {
        private static string Get(string key, string fallback = "")
            => ConfigurationManager.AppSettings[key] ?? fallback;

        public static void SendVerificationCode(string toEmail, string code)
        {
            var host = Get("Smtp:Host");
            var portStr = Get("Smtp:Port", "587");
            var enableSsl = Get("Smtp:EnableSsl", "true");
            var user = Get("Smtp:User");
            var pass = Get("Smtp:Pass");
            var from = Get("Smtp:From", user);
            var fromName = Get("Smtp:FromName", "Snake & Ladders");

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP no configurado (Smtp:Host).");

            int port = 587; int.TryParse(portStr, out port);
            bool ssl = true; bool.TryParse(enableSsl, out ssl);

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(from, fromName);
                msg.To.Add(new MailAddress(toEmail));
                msg.Subject = "Your verification code";
                msg.Body = new StringBuilder()
                    .AppendLine("Hi!")
                    .AppendLine()
                    .AppendLine($"Your verification code is: {code}")
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

                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    smtp.Send(msg);
                }
            }
        }
    }
}
