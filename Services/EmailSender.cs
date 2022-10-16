using ArmisApp.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ArmisApp.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            ToolsRepository Rep_Tools = new ToolsRepository();
            var qSetting = Rep_Tools.Settings();
            MailMessage msg = new MailMessage();
            msg.Body = message;
            msg.BodyEncoding = Encoding.UTF8;
            msg.From = new MailAddress(qSetting.Email, qSetting.Title, Encoding.UTF8);
            msg.IsBodyHtml = true;
            msg.Priority = MailPriority.Normal;
            msg.Sender = msg.From;
            msg.Subject = subject;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.To.Add(new MailAddress(email, email, Encoding.UTF8));

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "webmail.armisacademy.com";
            smtp.Port = 25;
            //smtp.Host = "smtp.gmail.com";
            //smtp.Port = 587; //465; 
            //smtp.EnableSsl = true;
            smtp.EnableSsl = false;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(qSetting.Email, qSetting.EmailPassword);
            //smtp.Credentials = new NetworkCredential("dereny60@gmail.com", "09364846160");

            smtp.Send(msg);
            return Task.CompletedTask;
        }
    }
}
