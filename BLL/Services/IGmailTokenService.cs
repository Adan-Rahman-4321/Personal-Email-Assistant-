namespace BLL.Services;

public interface IGmailTokenService
{
    Task<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    string CreateAuthorizationUrl(string state);
    Task ExchangeAuthorizationCodeAsync(string code, CancellationToken cancellationToken = default);
}
