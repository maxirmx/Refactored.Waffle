namespace Refactored.Waffle.EmailSender.Settings;

public sealed class EmailSendSettings
{
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? From { get; set; }
    public bool Ssl { get; set; }
}