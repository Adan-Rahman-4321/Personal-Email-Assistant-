namespace Models.Configuration;

/// <summary>
/// Configuration settings for email providers
/// </summary>
public class EmailProviderSettings
{
    public GmailSettings Gmail { get; set; } = new GmailSettings();
    public OutlookSettings Outlook { get; set; } = new OutlookSettings();
}

/// <summary>
/// Gmail API Settings
/// </summary>
public class GmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "Email Assistant";
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "https://localhost:7195/api/auth/gmail/callback";
    public bool IsConfigured => (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(UserEmail)) ||
                                !string.IsNullOrEmpty(AccessToken) ||
                                !string.IsNullOrEmpty(RefreshToken);
}

/// <summary>
/// Outlook Graph API Settings
/// </summary>
public class OutlookSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "https://localhost:5001/api/auth/outlook/callback";
    public bool IsConfigured => (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(TenantId)) || !string.IsNullOrEmpty(AccessToken);
}
