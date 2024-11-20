using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace CareerAI.Services;
public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void SendEmail(string to, string subject, string body)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");

        var smtpClient = new SmtpClient(smtpSettings["Host"])
        {
            Port = int.Parse(smtpSettings["Port"]),
            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
            EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
        };

        var fromAddress = new MailAddress(smtpSettings["Username"], smtpSettings["DisplayName"]);

        var mailMessage = new MailMessage
        {
            From = fromAddress,
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        smtpClient.Send(mailMessage);
    }
}