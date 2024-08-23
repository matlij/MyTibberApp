namespace MyTibber.Service.Options;

public class UpLinkCredentialsOptions
{
    public const string UpLinkCredentials = "UpLinkCredentials";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
