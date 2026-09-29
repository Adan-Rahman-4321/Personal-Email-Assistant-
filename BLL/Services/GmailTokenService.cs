using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Configuration;

namespace BLL.Services;

public sealed class GmailTokenService : IGmailTokenService
{
    private readonly HttpClient _httpClient;
    private readonly GmailSettings _settings;
    private readonly ILogger<GmailTokenService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public GmailTokenService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GmailTokenService> logger)
    {
        _httpClient = httpClient;
        _settings = new GmailSettings();
        configuration.GetSection("EmailProviders:Gmail").Bind(_settings);
        _logger = logger;
        _accessToken = string.IsNullOrWhiteSpace(_settings.AccessToken) ? null : _settings.AccessToken;
    }

    public async Task<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && !string.IsNullOrWhiteSpace(_accessToken) &&
            (_accessTokenExpiresAt == default || _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)))
        {
            return _accessToken;
        }

        if (string.IsNullOrWhiteSpace(_settings.RefreshToken))
        {
            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                return _accessToken;
            }

            throw new InvalidOperationException(
                "Gmail OAuth configuration requires EmailProviders:Gmail:AccessToken or RefreshToken.");
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && !string.IsNullOrWhiteSpace(_accessToken) &&
                (_accessTokenExpiresAt == default || _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)))
            {
                return _accessToken;
            }

            if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
            {
                throw new InvalidOperationException(
                    "Gmail OAuth configuration requires ClientId and ClientSecret to refresh the access token.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _settings.ClientId,
                    ["client_secret"] = _settings.ClientSecret,
                    ["refresh_token"] = _settings.RefreshToken,
                    ["grant_type"] = "refresh_token"
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var tokenResponse = await response.Content.ReadFromJsonAsync<GmailTokenResponse>(cancellationToken);
            if (!response.IsSuccessStatusCode || tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                _logger.LogError("Gmail access token refresh failed with status code {StatusCode}", response.StatusCode);
                throw new InvalidOperationException("Gmail access token refresh failed. Check the OAuth configuration and refresh token.");
            }

            _accessToken = tokenResponse.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60));
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public string CreateAuthorizationUrl(string state)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId))
        {
            throw new InvalidOperationException("Gmail ClientId is not configured.");
        }

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "https://www.googleapis.com/auth/gmail.readonly",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state
        };

        return "https://accounts.google.com/o/oauth2/v2/auth?" +
            string.Join("&", parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    public async Task ExchangeAuthorizationCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["redirect_uri"] = _settings.RedirectUri,
                ["grant_type"] = "authorization_code"
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var tokenResponse = await response.Content.ReadFromJsonAsync<GmailTokenResponse>(cancellationToken);
        if (!response.IsSuccessStatusCode || tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            _logger.LogError("Gmail authorization code exchange failed with status code {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Gmail authorization failed. Verify the OAuth redirect URI and client configuration.");
        }

        _accessToken = tokenResponse.AccessToken;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60));

        if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            _settings.RefreshToken = tokenResponse.RefreshToken;
        }
    }

    private sealed class GmailTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
