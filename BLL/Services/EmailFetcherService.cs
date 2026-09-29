using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Configuration;
using Models.DTOs;
using Models.Entities;
using Models.Enums;

namespace BLL.Services;

/// <summary>
/// Service for fetching emails from Gmail/Outlook APIs
/// </summary>
public class EmailFetcherService : IEmailFetcherService
{
    private readonly ILogger<EmailFetcherService> _logger;
    private readonly EmailProviderSettings _providerSettings;
    private readonly IGmailTokenService _gmailTokenService;

    public EmailFetcherService(
        ILogger<EmailFetcherService> logger,
        IConfiguration configuration,
        IGmailTokenService gmailTokenService)
    {
        _logger = logger;
        _gmailTokenService = gmailTokenService;
        
        // Load provider settings from configuration
        _providerSettings = new EmailProviderSettings();
        configuration.GetSection("EmailProviders").Bind(_providerSettings);
    }

    public async Task<IEnumerable<Email>> FetchEmailsAsync(EmailFetchRequest request)
    {
        _logger.LogInformation($"Fetching emails from {request.Provider}. Max: {request.MaxEmails}");

        try
        {
            return request.Provider.ToLower() switch
            {
                "gmail" => await FetchGmailEmailsAsync(request.MaxEmails, request.FromDate),
                "outlook" => await FetchOutlookEmailsAsync(request.MaxEmails, request.FromDate),
                _ => throw new ArgumentException($"Unsupported email provider: {request.Provider}. Supported providers are Gmail and Outlook.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching emails from {request.Provider}");
            throw;
        }
    }

    public async Task<IEnumerable<Email>> FetchGmailEmailsAsync(int maxEmails = 50, DateTime? fromDate = null)
    {
        _logger.LogInformation("Fetching emails from Gmail API...");

        if (string.IsNullOrEmpty(_providerSettings.Gmail.AccessToken) &&
            string.IsNullOrEmpty(_providerSettings.Gmail.RefreshToken))
        {
            _logger.LogError("Gmail OAuth tokens are missing. Configure AccessToken or RefreshToken.");
            throw new InvalidOperationException("Gmail OAuth AccessToken or RefreshToken is required in appsettings.json.");
        }
        
        if (!_providerSettings.Gmail.IsConfigured)
        {
            _logger.LogWarning("Gmail API configuration incomplete. Using AccessToken if provided.");
        }

        var emails = new List<Email>();
        
        try
        {
            using var httpClient = new HttpClient();
            
            // Gmail API uses "me" to refer to authenticated user
            var userEmail = "me"; // Use "me" instead of actual email
            var query = fromDate.HasValue ? $"after:{fromDate.Value:yyyy/MM/dd}" : "";
            
            // Fetch message list
            var messagesUrl = $"https://gmail.googleapis.com/gmail/v1/users/{userEmail}/messages?maxResults={maxEmails}";
            if (!string.IsNullOrEmpty(query))
            {
                messagesUrl += $"&q={Uri.EscapeDataString(query)}";
            }
            
            _logger.LogInformation($"Fetching messages from Gmail API for user: {userEmail}");
            
            var messagesResponse = await SendGmailRequestAsync(httpClient, messagesUrl);
            
            if (!messagesResponse.IsSuccessStatusCode)
            {
                var errorContent = await messagesResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Gmail API error: {messagesResponse.StatusCode} - {errorContent}");
                
                if (messagesResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Gmail API Authentication failed. Please provide a valid OAuth2 Access Token in appsettings.json under EmailProviders:Gmail:AccessToken");
                }
                
                throw new Exception($"Gmail API error: {messagesResponse.StatusCode} - {errorContent}");
            }
            
            var messagesJsonContent = await messagesResponse.Content.ReadAsStringAsync();
            var messagesJson = JsonDocument.Parse(messagesJsonContent);
            
            if (messagesJson.RootElement.TryGetProperty("messages", out var messagesArray))
            {
                var messageIds = messagesArray.EnumerateArray().Take(maxEmails);
                
                foreach (var messageItem in messageIds)
                {
                    try
                    {
                        var messageId = messageItem.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(messageId)) continue;
                        
                        // Fetch full message details
                        var messageUrl = $"https://gmail.googleapis.com/gmail/v1/users/{userEmail}/messages/{messageId}?format=full";
                         var messageResponse = await SendGmailRequestAsync(httpClient, messageUrl);
                        
                        if (messageResponse.IsSuccessStatusCode)
                        {
                            var messageJsonContent = await messageResponse.Content.ReadAsStringAsync();
                            var messageJson = JsonDocument.Parse(messageJsonContent);
                            
                            var email = ParseGmailMessage(messageJson.RootElement, messageId);
                            if (email != null)
                            {
                                emails.Add(email);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error fetching message {messageItem.GetProperty("id").GetString()}");
                    }
                }
            }
            
            _logger.LogInformation($"Successfully fetched {emails.Count} emails from Gmail");
            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching emails from Gmail API: {ex.Message}");
            // Don't fall back to mock - throw error so user knows Gmail API failed
            throw new Exception($"Error fetching emails from Gmail API. Please check your OAuth token and try again. Error: {ex.Message}", ex);
        }
    }

    private async Task<HttpResponseMessage> SendGmailRequestAsync(HttpClient httpClient, string url)
    {
        var accessToken = await _gmailTokenService.GetAccessTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized ||
            string.IsNullOrEmpty(_providerSettings.Gmail.RefreshToken))
        {
            return response;
        }

        response.Dispose();
        accessToken = await _gmailTokenService.GetAccessTokenAsync(forceRefresh: true);
        using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
        retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return await httpClient.SendAsync(retryRequest);
    }
    
    private Email? ParseGmailMessage(JsonElement messageJson, string messageId)
    {
        try
        {
            // Extract headers
            var payload = messageJson.GetProperty("payload");
            var headers = payload.GetProperty("headers");
            
            string subject = "";
            string from = "";
            string senderName = "Unknown";
            string senderEmail = "";
            DateTime receivedTime = DateTime.UtcNow;
            
            foreach (var header in headers.EnumerateArray())
            {
                var name = header.GetProperty("name").GetString() ?? "";
                var value = header.GetProperty("value").GetString() ?? "";
                
                switch (name.ToLower())
                {
                    case "subject":
                        subject = value;
                        break;
                    case "from":
                        from = value;
                        // Parse sender name and email
                        var fromMatch = Regex.Match(from, @"(.+?)\s*<(.+?)>|(.+@.+\.\w+)");
                        if (fromMatch.Groups[1].Success)
                        {
                            senderName = fromMatch.Groups[1].Value.Trim('"', ' ').Trim();
                            senderEmail = fromMatch.Groups[2].Value;
                        }
                        else if (fromMatch.Groups[3].Success)
                        {
                            senderEmail = fromMatch.Groups[3].Value;
                            senderName = senderEmail.Split('@')[0];
                        }
                        else
                        {
                            senderEmail = from;
                            senderName = "Unknown";
                        }
                        break;
                    case "date":
                        if (DateTimeOffset.TryParse(value, out var parsedDate))
                        {
                            receivedTime = parsedDate.UtcDateTime;
                        }
                        break;
                }
            }
            
            // Extract body
            string body = ExtractGmailBody(payload);
            
            var email = new Email
            {
                Subject = subject,
                Body = body,
                SenderName = senderName,
                SenderEmail = senderEmail,
                ReceivedTime = receivedTime,
                EmailProvider = "Gmail",
                ExternalEmailId = messageId,
                CreatedAt = DateTime.UtcNow
            };
            
            return email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gmail message");
            return null;
        }
    }
    
    private string ExtractGmailBody(JsonElement payload)
    {
        try
        {
            // Check if body exists directly
            if (payload.TryGetProperty("body", out var bodyElement) && 
                bodyElement.TryGetProperty("data", out var dataElement))
            {
                var base64Data = dataElement.GetString();
                if (!string.IsNullOrEmpty(base64Data))
                {
                    // Gmail uses URL-safe base64, convert to standard base64
                    base64Data = base64Data.Replace('-', '+').Replace('_', '/');
                    while (base64Data.Length % 4 != 0)
                    {
                        base64Data += "=";
                    }
                    
                    var bytes = Convert.FromBase64String(base64Data);
                    return Encoding.UTF8.GetString(bytes);
                }
            }
            
            // Check parts (for multipart messages)
            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    var mimeType = part.GetProperty("mimeType").GetString() ?? "";
                    
                    // Prefer plain text
                    if (mimeType == "text/plain" && part.TryGetProperty("body", out var partBody))
                    {
                        if (partBody.TryGetProperty("data", out var partData))
                        {
                            var base64Data = partData.GetString();
                            if (!string.IsNullOrEmpty(base64Data))
                            {
                                base64Data = base64Data.Replace('-', '+').Replace('_', '/');
                                while (base64Data.Length % 4 != 0)
                                {
                                    base64Data += "=";
                                }
                                
                                var bytes = Convert.FromBase64String(base64Data);
                                return Encoding.UTF8.GetString(bytes);
                            }
                        }
                    }
                }
                
                // Fallback to HTML if no plain text
                foreach (var part in parts.EnumerateArray())
                {
                    var mimeType = part.GetProperty("mimeType").GetString() ?? "";
                    if (mimeType == "text/html" && part.TryGetProperty("body", out var partBody))
                    {
                        if (partBody.TryGetProperty("data", out var partData))
                        {
                            var base64Data = partData.GetString();
                            if (!string.IsNullOrEmpty(base64Data))
                            {
                                base64Data = base64Data.Replace('-', '+').Replace('_', '/');
                                while (base64Data.Length % 4 != 0)
                                {
                                    base64Data += "=";
                                }
                                
                                var bytes = Convert.FromBase64String(base64Data);
                                var html = Encoding.UTF8.GetString(bytes);
                                // Strip HTML tags for now
                                return Regex.Replace(html, "<.*?>", string.Empty);
                            }
                        }
                    }
                }
            }
            
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Gmail body");
            return "";
        }

        /* 
        // Example implementation structure (when Google.Apis.Gmail.v1 is installed):
        
        using Google.Apis.Auth.OAuth2;
        using Google.Apis.Gmail.v1;
        using Google.Apis.Gmail.v1.Data;
        using Google.Apis.Services;
        
        // Create credential
        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = _providerSettings.Gmail.ClientId,
                ClientSecret = _providerSettings.Gmail.ClientSecret
            },
            new[] { GmailService.Scope.GmailReadonly },
            "user",
            CancellationToken.None).Result;

        // Create Gmail service
        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _providerSettings.Gmail.ApplicationName
        });

        // Fetch messages
        var request = service.Users.Messages.List("me");
        request.MaxResults = maxEmails;
        if (fromDate.HasValue)
        {
            // Add query filter for date
            request.Q = $"after:{fromDate.Value:yyyy/MM/dd}";
        }
        
        var messages = await request.ExecuteAsync();
        
        var emails = new List<Email>();
        
        if (messages.Messages != null)
        {
            foreach (var messageItem in messages.Messages.Take(maxEmails))
            {
                var messageDetail = await service.Users.Messages.Get("me", messageItem.Id).ExecuteAsync();
                
                // Parse email headers
                var subject = messageDetail.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "";
                var from = messageDetail.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "";
                var dateHeader = messageDetail.Payload.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;
                var receivedTime = DateTimeOffset.TryParse(dateHeader, out var parsedDate) 
                    ? parsedDate.UtcDateTime 
                    : DateTime.UtcNow;
                
                // Parse sender name and email
                var fromMatch = System.Text.RegularExpressions.Regex.Match(from, @"(.+?)\s*<(.+?)>|(.+@.+\.\w+)");
                var senderName = fromMatch.Groups[1].Success ? fromMatch.Groups[1].Value.Trim('"', ' ') 
                    : (fromMatch.Groups[3].Success ? fromMatch.Groups[3].Value.Split('@')[0] : "Unknown");
                var senderEmail = fromMatch.Groups[2].Success ? fromMatch.Groups[2].Value 
                    : (fromMatch.Groups[3].Success ? fromMatch.Groups[3].Value : from);
                
                // Extract body
                string body = "";
                if (messageDetail.Payload.Body?.Data != null)
                {
                    body = System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(messageDetail.Payload.Body.Data));
                }
                else if (messageDetail.Payload.Parts != null)
                {
                    foreach (var part in messageDetail.Payload.Parts)
                    {
                        if (part.MimeType == "text/plain" && part.Body?.Data != null)
                        {
                            body = System.Text.Encoding.UTF8.GetString(
                                System.Convert.FromBase64String(part.Body.Data));
                            break;
                        }
                    }
                }
                
                var email = new Email
                {
                    Subject = subject,
                    Body = body,
                    SenderName = senderName,
                    SenderEmail = senderEmail,
                    ReceivedTime = receivedTime,
                    EmailProvider = "Gmail",
                    ExternalEmailId = messageItem.Id,
                    CreatedAt = DateTime.UtcNow
                };
                
                emails.Add(email);
            }
        }
        
        return emails;
        */
    }

    public async Task<IEnumerable<Email>> FetchOutlookEmailsAsync(int maxEmails = 50, DateTime? fromDate = null)
    {
        _logger.LogInformation("Fetching emails from Outlook Graph API...");

        // Check if Outlook is configured
        if (!_providerSettings.Outlook.IsConfigured)
        {
            _logger.LogWarning("Outlook Graph API is not configured. Please add ClientId, ClientSecret, and TenantId in appsettings.json.");
            throw new InvalidOperationException("Outlook Graph API is not configured. Please add ClientId, ClientSecret, and TenantId (or AccessToken) in appsettings.json.");
        }

        if (!string.IsNullOrEmpty(_providerSettings.Outlook.AccessToken))
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_providerSettings.Outlook.AccessToken}");
            var url = $"https://graph.microsoft.com/v1.0/me/messages?$top={maxEmails}&$orderby=receivedDateTime desc";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Outlook API error: {response.StatusCode} - {err}");
            }
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var emails = new List<Email>();
            if (doc.RootElement.TryGetProperty("value", out var valueArray))
            {
                foreach (var item in valueArray.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                    var subject = item.TryGetProperty("subject", out var s) ? s.GetString() ?? "" : "";
                    var bodyPreview = item.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() ?? "" : "";
                    var received = item.TryGetProperty("receivedDateTime", out var rd) && rd.TryGetDateTime(out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow;
                    var senderName = "Outlook User";
                    var senderEmail = "";
                    if (item.TryGetProperty("from", out var fromProp) && fromProp.TryGetProperty("emailAddress", out var ea))
                    {
                        senderName = ea.TryGetProperty("name", out var n) ? n.GetString() ?? senderName : senderName;
                        senderEmail = ea.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
                    }
                    emails.Add(new Email
                    {
                        Subject = subject,
                        Body = bodyPreview,
                        SenderName = senderName,
                        SenderEmail = senderEmail,
                        ReceivedTime = received,
                        EmailProvider = "Outlook",
                        ExternalEmailId = id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            return emails;
        }

        throw new InvalidOperationException("Outlook Graph API AccessToken is required. Please add AccessToken in appsettings.json under EmailProviders:Outlook:AccessToken.");

        /*
        // Example implementation structure (when Microsoft.Graph packages are installed):
        
        using Microsoft.Graph;
        using Microsoft.Graph.Auth;
        using Microsoft.Identity.Client;
        
        // Create confidential client application
        var confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(_providerSettings.Outlook.ClientId)
            .WithTenantId(_providerSettings.Outlook.TenantId)
            .WithClientSecret(_providerSettings.Outlook.ClientSecret)
            .Build();

        // Create auth provider
        var authProvider = new ClientCredentialProvider(confidentialClientApplication);
        
        // Create Graph client
        var graphClient = new GraphServiceClient(authProvider);
        
        // Fetch messages
        var messagesRequest = graphClient.Me.Messages
            .Request()
            .Top(maxEmails)
            .OrderBy("receivedDateTime desc");
            
        if (fromDate.HasValue)
        {
            messagesRequest.Filter($"receivedDateTime ge {fromDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        
        var messages = await messagesRequest.GetAsync();
        
        var emails = new List<Email>();
        
        foreach (var message in messages)
        {
            var email = new Email
            {
                Subject = message.Subject ?? "",
                Body = message.Body?.Content ?? "",
                SenderName = message.From?.EmailAddress?.Name ?? "Unknown",
                SenderEmail = message.From?.EmailAddress?.Address ?? "",
                ReceivedTime = message.ReceivedDateTime?.DateTime ?? DateTime.UtcNow,
                EmailProvider = "Outlook",
                ExternalEmailId = message.Id ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
            
            emails.Add(email);
        }
        
        return emails;
        */
    }
}

