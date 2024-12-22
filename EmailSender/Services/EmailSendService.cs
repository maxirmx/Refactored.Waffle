// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

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
