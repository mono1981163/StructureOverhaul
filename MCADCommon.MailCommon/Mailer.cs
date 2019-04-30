using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
namespace MCADCommon.MailCommon
{
    public class Mailer
    {
        public MailSettings Settings { get; private set; }
        public Mailer(MailSettings settings)
        {
            Settings = settings;
        }

        public void Mail(string subject, string message, List<string> attachments)
        {
            MailMessage mail = new MailMessage { From = new MailAddress(Settings.Sender), Subject = subject, Body = message };
            foreach (string receiver in Settings.Receivers)
                mail.To.Add(new MailAddress(receiver));

            foreach (string attachment in attachments)
                mail.Attachments.Add(new Attachment(attachment));

            SmtpClient server = new SmtpClient(Settings.Host, Settings.Port);
            server.EnableSsl = Settings.UseTls;
            server.UseDefaultCredentials = false;
            server.Timeout = 20000;
            if (Settings.Sender != string.Empty && Settings.Password != string.Empty)
            { 
                server.Credentials = new System.Net.NetworkCredential(Settings.Sender, Settings.Password);
            }
            else
            {
                server.Credentials = new System.Net.NetworkCredential();
            }

            server.Send(mail);

        }
    }
}
