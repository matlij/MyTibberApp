namespace MyTibber.Common.Options;

public class UpLinkCredentialsOptions
{
    public const string UpLinkCredentials = "UpLinkCredentials";

    public string ClientIdentifier { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
