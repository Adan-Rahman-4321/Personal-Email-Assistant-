using Models.DTOs;
using Models.Entities;

namespace BLL.Services;

/// <summary>
/// Service interface for fetching emails from providers (Gmail/Outlook)
/// </summary>
public interface IEmailFetcherService
{
    Task<IEnumerable<Email>> FetchEmailsAsync(EmailFetchRequest request);
    Task<IEnumerable<Email>> FetchGmailEmailsAsync(int maxEmails = 50, DateTime? fromDate = null);
    Task<IEnumerable<Email>> FetchOutlookEmailsAsync(int maxEmails = 50, DateTime? fromDate = null);
}


