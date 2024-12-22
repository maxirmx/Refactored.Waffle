using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refactored.Waffle.EmailSender.Settings;


namespace Refactored.Waffle.EmailSender.Services
{
    public sealed class EmailSendService(IOptions<EmailSendSettings> options, 
                                         ILogger<EmailSendService> logger) : IEmailSendService
    {
        public async Task SendEmailAsync(string to, 
                                         string subject, 
                                         string body,
                                         CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(options.Value.Host) ||
                options.Value.Port == 0 ||
                string.IsNullOrEmpty(options.Value.From) ||
                string.IsNullOrEmpty(options.Value.Login) ||
                string.IsNullOrEmpty(options.Value.Password))
            {
                logger.LogWarning("SMTP settings are not set, skipping e-mail noitification");
            }
            else
            {
                var toAddressesSplit = to.Split(';');
                List<MailAddress> toAddresses = [];

                foreach (var ad in toAddressesSplit)
                {
                    var adTrim = ad.Trim();
                    if (!string.IsNullOrEmpty(adTrim))
                    {
                        try
                        {
                            toAddresses.Add(new MailAddress(adTrim));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError("Error processing e-mail address {ad}: {msg}", ad, ex.Message);
                        }
                    }
                }
                if (toAddresses.Count > 0)
                {
                    try
                    {
                        var fromAddress = new MailAddress(options.Value.From);

                        var smtp = new SmtpClient
                        {
                            Host = options.Value.Host,
                            Port = options.Value.Port,
                            EnableSsl = options.Value.Ssl,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(options.Value.Login, options.Value.Password)
                        };

                        var message = new MailMessage
                        {
                            From = fromAddress,
                            Subject = subject,
                            Body = body
                        };


                        foreach (var toAddress in toAddresses)
                        {
                            message.To.Add(toAddress);
                        }

                        await smtp.SendMailAsync(message, cancellationToken);
                        logger.LogInformation("E-mail sent successfully. Subject: {subj}", subject);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error sending e-mail: {msg}", ex.Message);
                    }
                }
                else
                {
                    logger.LogWarning("No valid e-mail addresses found, skipping e-mail notification");
                }
            }
        }
    }
}
