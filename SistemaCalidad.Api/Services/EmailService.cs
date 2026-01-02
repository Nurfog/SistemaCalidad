using System.Net;
using System.Net.Mail;

namespace SistemaCalidad.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        using var client = new SmtpClient(smtpServer, port)
        {
            Credentials = new NetworkCredential(senderEmail, password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail!, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
