using ArmisApp.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace NixShopApp.Models.Utility
{
    public class EmailService
    {
        public bool Send(string To, string Subject, string Text)
        {
            ToolsRepository Rep_Tools = new ToolsRepository();
            var qSetting = Rep_Tools.Settings();
            MailMessage msg = new MailMessage();
            msg.Body = Text;
            msg.BodyEncoding = Encoding.UTF8;
            msg.From = new MailAddress(qSetting.Email, qSetting.Title, Encoding.UTF8);
            msg.IsBodyHtml = true;
            msg.Priority = MailPriority.Normal;
            msg.Sender = msg.From;
            msg.Subject = Subject;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.To.Add(new MailAddress(To, To, Encoding.UTF8));

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587; //465; 
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(qSetting.Email, qSetting.EmailPassword);

            smtp.Send(msg);

            return true;
        }
    }
}
