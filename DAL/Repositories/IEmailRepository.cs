using Models.Entities;
using Models.DTOs;

namespace DAL.Repositories;

/// <summary>
/// Repository interface for Email operations
/// </summary>
public interface IEmailRepository
{
    Task<IEnumerable<Email>> GetAllEmailsAsync();
    Task<IEnumerable<Email>> GetEmailsByCategoryAsync(int categoryId);
    Task<IEnumerable<Email>> GetEmailsByCategoryNameAsync(string categoryName);
    Task<Email?> GetEmailByIdAsync(int id);
    Task<Email?> GetEmailByExternalIdAsync(string externalEmailId);
    Task<Email> AddEmailAsync(Email email);
    Task<Email> UpdateEmailAsync(Email email);
    Task<bool> DeleteEmailAsync(int id);
    Task<int> GetEmailCountByCategoryAsync(int categoryId);
    Task<bool> EmailExistsByExternalIdAsync(string externalEmailId);
}


