namespace Refactored.Waffle.EmailSender.Services
{
    public interface IEmailSendService
    {
        public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken);
    }
}
