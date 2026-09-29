using Models.DTOs;
using Models.Entities;

namespace BLL.Services;

/// <summary>
/// Service interface for processing and managing emails
/// </summary>
public interface IEmailProcessingService
{
    Task<IEnumerable<Email>> FetchAndClassifyEmailsAsync(EmailFetchRequest request);
    Task<Email> ProcessAndSaveEmailAsync(Email email);
    Task<IEnumerable<EmailDTO>> GetEmailsByCategoryAsync(string categoryName);
    Task<IEnumerable<CategoryDTO>> GetCategoriesWithCountsAsync();
    
    // CRUD Operations
    Task<IEnumerable<EmailDTO>> GetAllEmailsAsync();
    Task<EmailDTO?> GetEmailByIdAsync(int id);
    Task<EmailDTO?> UpdateEmailAsync(Models.DTOs.UpdateEmailRequest request);
    Task<bool> DeleteEmailAsync(int id);
    Task<EmailDTO?> ReclassifyEmailAsync(int id);
}

